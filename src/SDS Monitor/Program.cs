using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SDS_Monitor
{
	static class Program
	{
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main()
		{
			System.Threading.Thread.CurrentThread.Name = "MainThread";
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new FormMain());
		}
	}
}
