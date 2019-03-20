using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlusApplication1
{
    public class Program
    {
        public const bool CS_DESELECT = true;
        public const bool CS_SELECT = false;
        public const bool DC_COMMAND = false;
        public const bool DC_DATA = true;
        public const bool WR_WRITING = false;
        public const bool WR_COMPLETE = true;
        public const bool RD_READING = false;
        public const bool RD_COMPLETE = true;
        public const bool RESET_ACTIVE = false;
        public const bool RESET_COMPLETE = true;

        // DB0..7 Port
        public static OutputPort data0 = new OutputPort(Pins.GPIO_PIN_D0, false);
        public static OutputPort data1 = new OutputPort(Pins.GPIO_PIN_D1, false);
        public static OutputPort data2 = new OutputPort(Pins.GPIO_PIN_D2, false);
        public static OutputPort data3 = new OutputPort(Pins.GPIO_PIN_D3, false);
        public static OutputPort data4 = new OutputPort(Pins.GPIO_PIN_D4, false);
        public static OutputPort data5 = new OutputPort(Pins.GPIO_PIN_D13, false);
        public static OutputPort data6 = new OutputPort(Pins.GPIO_PIN_D6, false);
        public static OutputPort data7 = new OutputPort(Pins.GPIO_PIN_D7, false);

        // Control port
        public static OutputPort pinCS = new OutputPort(Pins.GPIO_PIN_D11, CS_DESELECT);
        public static OutputPort pinDC = new OutputPort(Pins.GPIO_PIN_D8, DC_COMMAND);
        public static OutputPort pinWR = new OutputPort(Pins.GPIO_PIN_D9, WR_WRITING);
        public static OutputPort pinRD = new OutputPort(Pins.GPIO_PIN_D10, !WR_WRITING);
        public static OutputPort pinRESET = new OutputPort(Pins.GPIO_PIN_D12, RESET_ACTIVE);

        public const int HDP800 = 800;
        public const int HT911 = 911;
        public const int HPS32 = 32;
        public const byte HPW7 = 7;        
        public const int LPS42 = 42;

        public const byte LPSPP0 = 0;

        public const int VDP480 = 480;
        public const int VT = 525;
        public const int VPS = 16;
        public const int FPS = 8;
        public const byte VPW = 16;

        public static void command(byte value)
        {
            pinDC.Write(DC_COMMAND);
            pinWR.Write(WR_WRITING);

            data0.Write((value & (byte)0x01) != 0);
            data1.Write((value & (byte)0x02) != 0);
            data2.Write((value & (byte)0x04) != 0);
            data3.Write((value & (byte)0x08) != 0);
            data4.Write((value & (byte)0x10) != 0);
            data5.Write((value & (byte)0x20) != 0);
            data6.Write((value & (byte)0x40) != 0);
            data7.Write((value & (byte)0x80) != 0);

            pinCS.Write(CS_SELECT);
            pinCS.Write(CS_DESELECT);
            pinWR.Write(WR_COMPLETE);
        }

        public static void data(byte value)
        {
            pinDC.Write(DC_DATA);
            pinWR.Write(WR_WRITING);

            data0.Write((value & (byte)0x01) != 0);
            data1.Write((value & (byte)0x02) != 0);
            data2.Write((value & (byte)0x04) != 0);
            data3.Write((value & (byte)0x08) != 0);
            data4.Write((value & (byte)0x10) != 0);
            data5.Write((value & (byte)0x20) != 0);
            data6.Write((value & (byte)0x40) != 0);
            data7.Write((value & (byte)0x80) != 0);

            pinCS.Write(CS_SELECT);
            pinCS.Write(CS_DESELECT); 
            pinWR.Write(WR_COMPLETE);
        }

        public static void data16(ushort value)
        {
            data((byte)(value >> 8));
            data((byte)(value & 0xFF));
        }

        public static void Main()
        {
            pinRESET.Write(RESET_ACTIVE);
            Thread.Sleep(5);
            pinRESET.Write(RESET_COMPLETE);
            Thread.Sleep(5);

            pinCS.Write(CS_DESELECT);
            pinRD.Write(RD_COMPLETE);
            pinWR.Write(WR_COMPLETE);
            Thread.Sleep(5);
            // LCD_CS = 0;  
            
            // Set interface 8 bit
            command(0xF0);
            data(0x00);
            Thread.Sleep(5);

            command(0x00E2);	// was PLL multiplier, set PLL clock to 120M
            data(0x0023);	    // 0x23 for 10M crystal, 35 * 10MHz crystal
            data(0x0003);       // M=20, so Frequency = 35 * 10MHz / 3 = 116 MHz, Divider (M) of PLL. (POR = 3)
            data(0x0004);       // Effectuate the multiplier and divider value
            command(0x00E0);    // PLL enable
            data(0x0001);
            Thread.Sleep(1);
            command(0x00E0);
            data(0x0003);       // use PLL as system clock

            Thread.Sleep(5);
            command(0x0001);    // software reset
            Thread.Sleep(5);
            command(0x00E6);    // PLL setting for PCLK, depends on resolution
            data(0x0001);       // 8.3MHz = PLL clock (116MHz) * value / 2^^20. D=74599
            data(0x0023);
            data(0x0067);

            command(0x00B0);	// LCD SPECIFICATION
            data(0x0020);       // 24-bit TFT data width, disable dithering, Dithering X, LSHIFT Raising, LLINE high, LFRAME high
            data(0x0020);       // Default: Hsync+Vsync+DE mode, TFT mode
            data16(HDP800 - 1); // Set HDP
            data16(VDP480 - 1); // Set VDP
            data(0x0000);       // Default: even line sequence RGB, odd line sequence RGB

            command(0x00B4);	         //HSYNC
            data((HT911 >> 8) & 0X00FF); // Set HT horizontal total period (POR343 + 1 = 344)
            data(HT911 & 0X00FF);
            data((HPS32 >> 8) & 0X00FF); // HPS non-display period between the start of the horizontal sync (LLINE) signal and 
            data(HPS32 & 0X00FF);        // the first display data. (POR = 32)
            data(HPW7);			         // HPW=7, horizontal sync pulse width (LLINE) in pixel clock. (POR = 7)
                                         // Horizontal Sync Pulse Width = (HPW + 1) pixels
            data((LPS42 >> 8) & 0X00FF); // LPS=99, horizontal sync pulse width (LLINE) in start. (POR = 0)
            data(LPS42 & 0X00FF);        // Horizontal Display Period Start Position = LPS pixels
            data(LPSPP0);                // LPSPP = 0, horizontal sync pulse subpixel start position (POR = 00)

            command(0x00B6);	         //VSYNC
            data((VT >> 8) & 0X00FF);    //Set VT
            data(VT & 0X00FF);
            data((VPS >> 8) & 0X00FF);   //Set VPS
            data(VPS & 0X00FF);
            data(VPW);			         //Set VPW
            data((FPS >> 8) & 0X00FF);   //Set FPS
            data(FPS & 0X00FF);

            command(0x00BA);
            data(0x000F);    //GPIO[3:0] out 1

            command(0x00B8);
            data(0x0007);    //GPIO3=input, GPIO[2:0]=output
            data(0x0001);    //GPIO0 normal

            command(0x0034); //TE off

            command(0x0036); //rotation
            data(0x0000);
            Thread.Sleep(100);

            // Disable postprocessor
            command(0xBC);
            data(0x40);
            data(0x80);
            data(0x40);
            data(0x00);

            command(0x00F0); //pixel data interface
            data(0x0000); // 8bit

            Thread.Sleep(100);
            command(0x0029); //display on
            Thread.Sleep(120);

            //command(0x00d0);
            //data(0x000d);

            // 8-bit per pixel
            //command(0x3A);
            //data(0x04);


            //command(0x2C);
            //data(0x50);
            //data(0x10);
            //data(0xF0);

            Paint();
            while (true)
            {
                //data(0);
            }
        }

        public static void Paint()
        {
            int i, j;
            Address_set(0, 0, 799, 479);
            for (i = 0; i < 480; i++)
            {
                for (j = 0; j < 800; j++)
                {
                    data((byte)(j & 255));
                    data((byte)((i + j) & 255));
                    data((byte)(i & 255));
                    
                }
            }
        }

        public static void Address_set(ushort x1, ushort y1, ushort x2, ushort y2)
        {

            command(0x002A);
            data((byte)((x1 >> 8) & 0xff));
            data((byte)(x1 & 0xff));
            data((byte)((x2 >> 8) & 0xff));
            data((byte)(x2 & 0xff));
            command(0x002b);
            data((byte)((y1 >> 8) & 0xff));
            data((byte)(y1 & 0xff));
            data((byte)((y2 >> 8) & 0xff));
            data((byte)(y2 & 0xff));
            command(0x002c);
        }

    }
}
