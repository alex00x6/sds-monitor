using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;

namespace SDS_Monitor
{
	public partial class FormMain : Form
	{
		DataTable dt_comports = new DataTable();
		DataTable dt_gatedb = new DataTable();
		DataTable dt_targets = new DataTable();
		Stopwatch sw = new Stopwatch();
		long l_prevtime;

		private SystemMenu m_SystemMenu = null;
		private const int m_AboutID = 0x100;

		public FormMain()
		{
			InitializeComponent();

			DataRow row;
			// COMポート一覧のDataTableを作成
			dt_comports.Columns.Add("DeviceID", typeof(string));
			dt_comports.Columns.Add("Caption", typeof(string));

			// 搭載COMポートを列挙する
			foreach (COMPortInfo p in COMPortInfo.GetCOMPortsInfo())
			{
				row = dt_comports.NewRow();
				row["DeviceID"] = (string)p.Name;
				row["Caption"] = (string)p.Description;
				dt_comports.Rows.Add(row);
			}

			// ComboBoxに登録
			cb_COMPORT.DataSource = dt_comports;
			cb_COMPORT.DisplayMember = "Caption";
			cb_COMPORT.ValueMember = "DeviceID";


			dt_targets.Columns.Add("Address", typeof(string));
			dt_targets.Columns.Add("Caption", typeof(string));
			
			row = dt_targets.NewRow();
			row["Address"] = "12";
			row["Caption"] = "ECU";
			dt_targets.Rows.Add(row);
			row = dt_targets.NewRow();
			row["Address"] = "01";
			row["Caption"] = "ECU/Transmission";
			dt_targets.Rows.Add(row);
			row = dt_targets.NewRow();
			row["Address"] = "19";
			row["Caption"] = "Transmission";
			dt_targets.Rows.Add(row);
			row = dt_targets.NewRow();
			row["Address"] = "29";
			row["Caption"] = "ABS";
			dt_targets.Rows.Add(row);
			row = dt_targets.NewRow();
			row["Address"] = "31";
			row["Caption"] = "EPS";
			dt_targets.Rows.Add(row);

			cbTarget.DataSource = dt_targets;
			cbTarget.DisplayMember = "Caption";
			cbTarget.ValueMember = "Address";


			dgv_Data.Rows.Add(4);

			toolTip1.SetToolTip(aG_ECT, "Engine coolant temperature");
			toolTip1.SetToolTip(aG_IAP, "Manifold absolute pressure");
			toolTip1.SetToolTip(aG_IAT, "Intake air temperature");
			toolTip1.SetToolTip(aG_RPM, "Engine speed");
			toolTip1.SetToolTip(aG_TP, "Throttle position");
			toolTip1.SetToolTip(lb_EAP, "Barometric pressure");
			toolTip1.SetToolTip(lb_STP, "Secondery throttle actuator position");
			toolTip1.SetToolTip(lb_VBAT, "Battery voltage");
			toolTip1.SetToolTip(led_CLT, "Clutch switch signal");
			toolTip1.SetToolTip(led_EXC, "Exhaust control valve actuator");
			toolTip1.SetToolTip(led_EXS, "Exhaust valve contrtol selector");
			toolTip1.SetToolTip(led_NT, "Neutral switch signal");
			toolTip1.SetToolTip(led_SRL, "Starter relay signal");
			toolTip1.SetToolTip(lb_MODE, "Driving mode select switch");
			toolTip1.SetToolTip(led_FAN, "Cooling FAN relay");
			toolTip1.SetToolTip(cbTarget, "choose an ECU/Transmission for SECVT scooter, an ECU for most others");

			System.Type dgvtype = typeof(DataGridView);
			System.Reflection.PropertyInfo dgvPropertyInfo =
				  dgvtype.GetProperty(
				  "DoubleBuffered", System.Reflection.BindingFlags.Instance |
				  System.Reflection.BindingFlags.NonPublic);
			dgvPropertyInfo.SetValue( dgv_Data, true, null);

			dgv_Data[0, 0].Value = "000";
			dgv_Data[0, 1].Value = "010";
			dgv_Data[0, 2].Value = "020";
			dgv_Data[0, 3].Value = "030";

			for (int c = 1; c < dgv_Data.ColumnCount; c++)
				for (int r = 0; r < dgv_Data.RowCount; r++)
					dgv_Data[c, r].Style.BackColor = Color.White;

			m_SystemMenu = SystemMenu.FromForm(this);
			m_SystemMenu.AppendSeparator();
			m_SystemMenu.AppendMenu(m_AboutID, "About SDS Monitor");

			ckb_LogAllCommunication.Enabled = ckb_EnableLogging.Checked;
		}

		System.Threading.Thread th;

		private void btn_Connect_Click(object sender, EventArgs e)
		{

			ElmLogger.Configure(ckb_EnableLogging.Checked, ckb_LogAllCommunication.Checked);

			string str;

			if (!IsOpened)
			{
				if (ckb_WIFI.Checked)
				{
					tb_IP.Enabled = false;
					tb_PORT.Enabled = false;
					str = tb_IP.Text;
				}
				else
				{
					cb_COMPORT.Enabled = false;
					str = cb_COMPORT.SelectedValue.ToString();
				}
				btn_Connect.Enabled = false;
				cbTarget.Enabled = false;
				lb_ECUID.Text = ""; lb_ELM.Text = "";

				if (!ELM_Open(str))
				{
					if (ckb_WIFI.Checked)
					{
						tb_IP.Enabled = true;
						tb_PORT.Enabled = true;
					}
					else
					{
						cb_COMPORT.Enabled = true;
					}
					btn_Connect.Text = "Connect";
					btn_Connect.Enabled = true;

					return;
				}

				Cursor preCursor = Cursor.Current;
				Cursor.Current = Cursors.WaitCursor;

				if ((str = ELM_Send_with_log("ATZ\r")) == null) goto Error;
				if((str = ELM_Send_with_log("ATE0\r")) == null) goto Error;
                if ((str = ELM_Send_with_log("ATSP5\r")) == null) goto Error;
                if ((str = ELM_Send_with_log("ATI\r")) == null) goto Error;
				lb_ELM.Text = str;
				if((str = ELM_Send_with_log("ATWM80" + cbTarget.SelectedValue + "F1013E\r")) == null) goto Error;
				if ((str = ELM_Send_with_log("ATSH81" + cbTarget.SelectedValue + "F1\r")) == null) goto Error;
                if ((str = ELM_Send_with_log("ATFI\r")) == null) goto Error;
				if (!str.Contains("OK")) goto Error;
				if ((str = ELM_Send_with_log("ATSH80" + cbTarget.SelectedValue + "F1\r")) == null) goto Error;

				if ((str = ELM_Send_with_log("1A9A\r")) == null) goto Error;
				string[] data = str.Split(' ');
				str = "";
				for (int i = 2; i < 12; i++)
				{
					str += Convert.ToChar(Convert.ToInt32(data[i], 16));
				}
				lb_ECUID.Text = str;
				System.Diagnostics.Debug.WriteLine(str);
				Cursor.Current = preCursor;

				btn_Connect.Text = "Disconnect";
				ckb_WIFI.Enabled = false;
				btn_Connect.Enabled = true;
				l_prevtime = 0;
				sw.Start();
				tDecay.Enabled = true;

				th = new System.Threading.Thread(CaptureLoop);
				th.Start();
			}
			else
			{
				IsOpened = false;
				tDecay.Enabled = false;

				Cursor preCursor = Cursor.Current;
				Cursor.Current = Cursors.WaitCursor;
				while (th.IsAlive)
					Application.DoEvents();

				sw.Stop();
				lb_fps.Text = "fps";
				str = ELM_Send("82\r");
				str = ELM_Send("ATPC\r");

				for (int r = 0; r < dgv_Data.RowCount; r++)
				{
					for (int c = 1; c < dgv_Data.ColumnCount; c++)
					{
						dgv_Data[c, r].Value = "";
						dgv_Data[c, r].Style.BackColor = Color.White;
					}
				}
				aG_RPM.Value = 0; aG_TP.Value = 0; aG_IAP.Value = 0;
				aG_ECT.Value = 0; aG_IAT.Value = 0;
				lb_VBAT.Text = ""; lb_Gear.Text = "-"; lb_EAP.Text = ""; lb_STP.Text = "";
				led_EXC.On = false; led_CLT.On = false; led_SRL.On = false;
				led_NT.On = false; led_EXS.On = false;
				lb_ECUID.Text = ""; lb_ELM.Text = "";
				btn_Connect.Text = "Connect";

				if (ckb_WIFI.Checked)
				{
					tb_IP.Enabled = true;
					tb_PORT.Enabled = true;
				}
				else 
				{
					cb_COMPORT.Enabled = true;
				}

				ckb_WIFI.Enabled = true;
				Cursor.Current = preCursor;

				goto Close;
			}

			return;

Error:
			string logPath = ElmLogger.GetCurrentLogPath();
			if (!string.IsNullOrEmpty(logPath))
				MessageBox.Show("Initialize Error. ELM log file: " + logPath, "SDS Monitor - Initialize Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			else
				MessageBox.Show("Initialize Error", "SDS Monitor - Initialize Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			cb_COMPORT.Enabled = true;
			btn_Connect.Enabled = true;
			cbTarget.Enabled = true;
Close:
			serialPort1.Close();
			if(tcp != null) tcp.Close();
			if(ns != null) ns.Close();
			if(ms != null) ms.Close();
			IsOpened = false;

			return;
		}

		private void UpdateGauge(string str)
		{
			try
			{
				string[] data = str.Split(' ');
				int i = 0;
				for (int r = 0; r < dgv_Data.RowCount; r++)
				{
					for (int c = 1; c < dgv_Data.ColumnCount; c++)
					{
						string v1 = data[i].ToLower(); string v2 = (string)dgv_Data[c, r].Value;
						if (v1 != v2)
						{
							if (v2 != "" && v2 != null)
								dgv_Data[c, r].Style.BackColor = Color.FromArgb(0xff, 0x80, 0x80);

							dgv_Data[c, r].Value = v1;
						}

						if (++i > 51) break;
					}
				}

				aG_RPM.Value = (float)(Convert.ToInt32(data[13] + data[14], 16)) / 2550F;
				rpmText.Text = (aG_RPM.Value*1000).ToString("0"); ;

                aG_TP.Value = (float)(Convert.ToInt32(data[15], 16)) * 125F / 255F;
				tpText.Text = aG_TP.Value.ToString("0.0");

				aG_IAP.Value = (float)(Convert.ToInt32(data[16], 16)) * 166.7F / 255F - 20F;
				iapText.Text = aG_IAP.Value.ToString("0");

				aG_ECT.Value = (float)(Convert.ToInt32(data[17], 16)) * 160F / 255F - 30F;
				ectText.Text = aG_ECT.Value.ToString("0");

                aG_IAT.Value = (float)(Convert.ToInt32(data[18], 16)) * 160F / 255F - 30F;
				iatText.Text = aG_IAT.Value.ToString("0");

				lb_EAP.Text = ((float)(Convert.ToInt32(data[19], 16)) * 1667F / 255F - 200F).ToString("0");
				lb_VBAT.Text = ((float)(Convert.ToInt32(data[20], 16)) * 20F / 255F).ToString("0.0");
				int gear = Convert.ToInt32(data[22], 16);
				if (gear > 6) gear = 7;
				lb_Gear.Text = ("N123456-").Substring(gear, 1);
				lb_STP.Text = ((float)(Convert.ToInt32(data[42], 16)) * 100F / 255F).ToString("0.0");

				int b = Convert.ToInt32(data[46], 16);
				if ((b & 0x30) == 0x10)
					lb_MODE.Text = "DOWN";
				else if ((b & 0x30) == 0x20)
					lb_MODE.Text = "UP";
				else if ((b & 0x30) == 0x30)
					lb_MODE.Text = "";
				led_FAN.On = (b & 4) == 0 ? false : true;
				led_EXC.On = (data[47] == "00") ? false : true;
				b = Convert.ToInt32(data[48], 16);
				led_CLT.On = (b & 16) == 0 ? false : true;
				led_SRL.On = (b & 32) == 0 ? false : true;
				b = Convert.ToInt32(data[49], 16);
				led_NT.On = (b & 2) == 0 ? true : false;
				led_EXS.On = (b & 8) == 0 ? true : false;

			}
			catch(Exception ex)
			{
				MessageBox.Show("Parse Error", "SDS Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
				errorstop();
			}

			lb_fps.Text = (1000.0F / (sw.ElapsedMilliseconds - l_prevtime)).ToString("N1") + " fps";
			l_prevtime = sw.ElapsedMilliseconds;
		}

		private void errorstop()
		{
			btn_Connect.Text = "Connect";
			ckb_WIFI.Enabled = true;
			if (ckb_WIFI.Checked)
			{
				tb_IP.Enabled = true;
				tb_PORT.Enabled = true;

			} else
				cb_COMPORT.Enabled = true;

			btn_Connect.Enabled = true;
			cbTarget.Enabled = true;
			serialPort1.Close();
			if (tcp != null) tcp.Close();
			if (ns != null) ns.Close();
			if (ms != null) ms.Close();
			IsOpened = false;
		}

		delegate void update_delegate(string str);
		delegate void errorstop_delegate();

		private void CaptureLoop()
		{
			System.Threading.Thread.CurrentThread.Name = "CaptureLoopThread";
			string str = "";

			while (IsOpened)
			{
				str = ELM_Send("2108\r");
				if (str == null || str == "BUS ERROR")
				{
					MessageBox.Show("ELM BUS ERROR", "SDS Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
					errorstop_delegate d = new errorstop_delegate(errorstop);
					Invoke(d);

					return;
				}

				if (this.InvokeRequired)
				{
					update_delegate d = new update_delegate(UpdateGauge);
					Invoke(d, new object[] { str });
				}
				else
					UpdateGauge(str);
			}
		}

		TcpClient tcp;
		NetworkStream ns;
		MemoryStream ms;
		bool IsOpened = false, IsTCP = false;

		private bool ELM_Open(string port)
		{
			Cursor preCursor = Cursor.Current;
			try
			{
				if (port.Substring(0, 3) == "COM")
				{
					serialPort1.PortName = port;
					serialPort1.BaudRate = 38400;
					serialPort1.Parity = Parity.None;
					serialPort1.StopBits = StopBits.One;
					serialPort1.DataBits = 8;
					serialPort1.Handshake = Handshake.None;
					serialPort1.NewLine = "\r\r>";
					serialPort1.ReadTimeout = 3000;
					serialPort1.Open();

					IsTCP = false;
				}
				else
				{
					IPAddress addr = IPAddress.Parse(port);
					tcp = new TcpClient();
					tcp.ReceiveTimeout = 3000; tcp.SendTimeout = 3000;
					Cursor.Current = Cursors.WaitCursor;
					tcp.Connect(addr, Convert.ToInt32(tb_PORT.Text));
					Cursor.Current = preCursor;
					ns = tcp.GetStream();
					ns.ReadTimeout = 3000;
					IsTCP = true;
				}
			}
			catch(Exception ex)
			{
				Cursor.Current = preCursor;
				MessageBox.Show(ex.Message, "SDS Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);

				return false;
			}

			IsOpened = true; return true;
		}

		private string ELM_Send_with_log(string str)
		{
			string tstr = str;
            tstr = tstr.Trim();
            tstr = tstr.Replace("\r", "");
            ElmLogger.WriteConnection($"Request to ELM \t > {tstr} ");

            string rstr = "";

			if (IsTCP)
			{
				try
				{
					UTF8Encoding enc = new UTF8Encoding();
					byte[] buf = enc.GetBytes(str);
					ns.Write(buf, 0, buf.Length);

					int i = 0;
					buf = new byte[256];
					enc = new UTF8Encoding();

					while (i < buf.GetLength(0))
					{
						buf[i++] = (byte)ns.ReadByte();
						if (i > 2 && buf[i - 1] == '>' && buf[i - 2] == 0x0d && buf[i - 3] == 0x0d)
						{
							rstr = enc.GetString(buf);
							break;
						}
					};
				}
				catch (Exception ex)
				{
					tb_IP.Enabled = true;
					tb_PORT.Enabled = true;
					goto Error;
				}

			}
			else
			{
				try
				{
					serialPort1.Write(str);
					rstr = serialPort1.ReadLine();
				}
				catch (Exception ex)
				{
					cb_COMPORT.Enabled = true;
					goto Error;
				}

            }

            string trstr = rstr;
            trstr = trstr.Trim();
            trstr = trstr.Replace("\r", " ");
            trstr = trstr.Replace("\0", "");
            trstr = trstr.Replace(">", "");
            ElmLogger.WriteConnection($"Response from ELM \t < {trstr} ");

            return rstr;

Error:
			MessageBox.Show("Communication Timeout", "SDS Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
			btn_Connect.Enabled = true;
			cbTarget.Enabled = true;

			return (string)null;
		}

        private string ELM_Send(string str)
        {
            string tstr = str;
            tstr = tstr.Trim();
            tstr = tstr.Replace("\r", "");
            ElmLogger.WriteCommunication($"Request to ELM \t > {tstr} ");

            string rstr = "";

            if (IsTCP)
            {
                try
                {
                    UTF8Encoding enc = new UTF8Encoding();
                    byte[] buf = enc.GetBytes(str);
                    ns.Write(buf, 0, buf.Length);

                    int i = 0;
                    buf = new byte[256];
                    enc = new UTF8Encoding();

                    while (i < buf.GetLength(0))
                    {
                        buf[i++] = (byte)ns.ReadByte();
                        if (i > 2 && buf[i - 1] == '>' && buf[i - 2] == 0x0d && buf[i - 3] == 0x0d)
                        {
                            rstr = enc.GetString(buf);
                            break;
                        }
                    };
                }
                catch (Exception ex)
                {
                    tb_IP.Enabled = true;
                    tb_PORT.Enabled = true;
                    goto Error;
                }

            }
            else
            {
                try
                {
                    serialPort1.Write(str);
                    rstr = serialPort1.ReadLine();
                }
                catch (Exception ex)
                {
                    cb_COMPORT.Enabled = true;
                    goto Error;
                }

            }

            string trstr = rstr;
            trstr = trstr.Trim();
            trstr = trstr.Replace("\r", " ");
            trstr = trstr.Replace("\0", "");
            trstr = trstr.Replace(">", "");
            ElmLogger.WriteCommunication($"Response from ELM \t < {trstr} ");

            return rstr;

        Error:
            MessageBox.Show("Communication Timeout", "SDS Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btn_Connect.Enabled = true;
            cbTarget.Enabled = true;

            return (string)null;
        }

        private void ELM_Close()
		{
			if (IsTCP)
			{
				if(ns != null)
					ns.Close();
				if(tcp != null && tcp.Connected)
					tcp.Close();
			}
			else
			{
				serialPort1.Close();
			}

			IsOpened = false;
		}

		private void ckb_WIFI_CheckedChanged(object sender, EventArgs e)
		{
			if (ckb_WIFI.Checked)
			{
				cb_COMPORT.Enabled = false;
				tb_IP.Enabled = true; tb_PORT.Enabled = true;
			}
			else
			{
				cb_COMPORT.Enabled = true;
				tb_IP.Enabled = false; tb_PORT.Enabled = false;
			}
		}

		private void ckb_EnableLogging_CheckedChanged(object sender, EventArgs e)
		{
			ckb_LogAllCommunication.Enabled = ckb_EnableLogging.Checked;
			if (!ckb_EnableLogging.Checked)
				ckb_LogAllCommunication.Checked = false;
		}

		private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (IsOpened) btn_Connect_Click(null, null);
		}

		private void tDecay_Tick(object sender, EventArgs e)
		{
			for (int c = 1; c < dgv_Data.ColumnCount; c++)
			{
				for (int r = 0; r < dgv_Data.RowCount; r++)
				{
					if (dgv_Data[c, r].Style.BackColor.B < 0xf0)
					{
						Color color = Color.FromArgb(dgv_Data[c, r].Style.BackColor.R,
							dgv_Data[c, r].Style.BackColor.G + 0x10,
							dgv_Data[c, r].Style.BackColor.B + 0x10);
						dgv_Data[c, r].Style.BackColor = color;
					}
					else if (dgv_Data[c, r].Style.BackColor.B != 0xff)
					{
						dgv_Data[c, r].Style.BackColor = Color.White;
					}
				}
			}
		}

		private void rpmText_TextChanged(object sender, EventArgs e)
		{

		}

		private void tpText_TextChanged(object sender, EventArgs e)
		{

		}

        private void ckb_LogAllCommunication_CheckedChanged(object sender, EventArgs e)
        {

        }

        protected override void WndProc ( ref Message msg )
{
   // Now we should catch the WM_SYSCOMMAND message and process it.
   // We override the WndProc to catch the WM_SYSCOMMAND message.
   // You don't have to look this message up in the MSDN; it is
   // defined in WindowMessages enumeration.
   // The WParam of the message contains the ID that was pressed.
   // It is the same value you have passed through InsertMenu()
   // or AppendMenu() member functions of my class.
   // Just check for them and do the proper action.
   //
      if ( msg.Msg == (int)WindowMessages.wmSysCommand )
      {
         switch ( msg.WParam.ToInt32() )
         {
            case m_AboutID:
               { // Our about id
                  MessageBox.Show(this, "SDS Monitor Version 1.2\r\r2026(c) Kashi Electronics Designs\rkashima@kaele.com", "About SDS Monitor");
               } break;

               // TODO: Add more handles, for more menu items

         }
      }
      // Call base class function
      base.WndProc(ref msg);
}
	}
}
