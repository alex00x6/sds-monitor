# Suzuki SDS Monitor

![](/img/screenshot.png)

Tool to display data from Suzuki's ECU via ELM327 over K-Line  
Original version wasn't working with my adapter very well, so I had to change a few things. Also added few improvements while I was at it.  
Thankfully, author posted source files, big kudos to him.

## Setup

How to make it work with ELM327 WiFi:  
**1** - Connect 3 wires from OBD connector to 6 pin Suzuki connector  
**2** - Turn on the ignition  
**3** - Connect to ELM327 WiFi hotspot   
**4** - Launch .exe file  
**5** - Tick WiFi OBD checkbox  
**6** - Check if IP and Port values are correct  
**7** - Click connect  

## Wiring

![](/img/wiring-obd.jpg)  
![](/img/wiring-suzuki.jpg)

## Notes

Tested on Suzuki GSR 600 2007

Verified with this ELM327 adapter (likely would work with others):

![](/img/obd_adapter.jpg)

## Links

Original SDS Monitor v1.01 & Article: http://kaele.com/~kashima/car/busa/ecu.html

List of ELM327 commands: https://cdn.sparkfun.com/assets/4/e/5/0/2/ELM327_AT_Commands.pdf
