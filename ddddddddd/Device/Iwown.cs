
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GeekSync4Band.Model;

namespace GeekSync4Band.Device
{
    class Iwown : IDevice
    {
       // private const int WaitDelay = 0x3e8;
        public bool IsOpen
        {
            get
            {
                return VSerialPort.IsOpen;
            }
        }

        #region CRC
        //done
        private string AddCRC(string dataStr)
        {
            byte[] buffer = HexStr2ByteArr(dataStr);
            byte num = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                num = (byte)(num ^ buffer[i]);
            }
            return (num.ToString("X") + " " + dataStr);
        }

        private string CalcCRC_Two(IEnumerable<byte> inputByteArr)
        {
            var num = inputByteArr.Aggregate(0, (current, t) => current ^ t);
            var str = num.ToString("X");
            return str.Length <= 2 ? str : str.Substring(str.Length - 2, 2);
        }

        private string CalcCRC_One(byte[] inputByteArr)
        {
            var num = 0;
            for (var i = 1; i < inputByteArr.Length; i++)
            {
                num += inputByteArr[i] ^ i;
            }
            var str = num.ToString("X");
            if (str.Length <= 2)
            {
                return str;
            }
            return str.Substring(str.Length - 2, 2);
        }

        private byte CalcCRC(string dataStr)
        {
            byte[] buffer = HexStr2ByteArr(dataStr);
            byte num = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                num = (byte)(num ^ buffer[i]);
            }
            return num;
        }

        private string PrepareHexStr_1(string parameter8)
        {
            var str = CalcCRC_One(HexStr2ByteArr("F5 " + parameter8));
            var inputString = "08 F5 " + parameter8 + " " + str;
            return (CalcCRC_Two(HexStr2ByteArr(inputString)) + " " + inputString);

        }
        private string PrepareHexStr_2(string parameter9)
        {
            var str = CalcCRC_One(HexStr2ByteArr("F5 00 " + parameter9));
            var inputString = "13 F5 00 " + parameter9 + " " + str;
            return (CalcCRC_Two(HexStr2ByteArr(inputString)) + " " + inputString);

        }
        private string PrepareHexStr_3(string parameter10)
        {
            var str = CalcCRC_One(HexStr2ByteArr("F5 01 " + parameter10));
            var inputString = "13 F5 01 " + parameter10 + " " + str;
            return (CalcCRC_Two(HexStr2ByteArr(inputString)) + " " + inputString);

        }
      
        #endregion

#region Get Function
        public override string GetTime()
        {
            string str;
            try
            {
                Send("AA 80 16 F0 00 00");
                var tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                var str2 = ReturnData.Trim();
                var str3 = str2.Substring(14, 2);
                str3 = "00" + Convert.ToInt32(str3, 0x10);
                str3 = "20" + str3.Substring(str3.Length - 2, 2);
                var str4 = str2.Substring(0x10, 2);
                str4 = (1 + Convert.ToInt32(str4, 0x10)).ToString(CultureInfo.InvariantCulture);
                var str5 = str2.Substring(0x12, 2);
                str5 = (1 + Convert.ToInt32(str5, 0x10)).ToString(CultureInfo.InvariantCulture);
                var str6 = Convert.ToInt32(str2.Substring(20, 2), 0x10).ToString(CultureInfo.InvariantCulture);
                var str7 = Convert.ToInt32(str2.Substring(0x16, 2), 0x10).ToString(CultureInfo.InvariantCulture);
                var str8 = Convert.ToInt32(str2.Substring(0x18, 2), 0x10).ToString(CultureInfo.InvariantCulture);
                str = str3 + "-" + str4 + "-" + str5 + " " + str6 + ":" + str7 + ":" + str8;
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return str;
        }
        public override string GetBand()
        {
            var str = "";
            try
            {
                Send("AA 80 10 F0 00 00");
                var tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < WaitDelay) && IsReciving)
                {
                    Application.DoEvents();
                }
                if (ReturnData.Length > 30)
                {
                    var bytes = HexStr2ByteArr(ReturnData.Substring(14, 12));
                    str = Encoding.Default.GetString(bytes);
                }
            }
            catch
            {
                str = "";
            }
            return str;
        }
        public override string GetBmac()
        {
            try
            {
                Send("AA 80 15 F0 00 00");
                var tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                var str2 = ReturnData.Trim();
                if (str2.Length > 0x1a)
                {
                    return str2.Substring(14, 12);
                }
                return "";
            }
            catch
            {
                return "";
            }
        }
        public string GetLed()
        {
            try
            {
                Send("AA 80 04 FF 00 00");
                int tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < WaitDelay) && IsReciving)
                {
                    Application.DoEvents();
                }
                return ReturnData.Trim();
            }
            catch
            {
                return "";
            }
        }
        public string GetSitConfig()
        {
            string str = "";
            try
            {
                Send("AA 80 1B F0 00 00");
                int tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                ReturnData.Trim();
                if (ReturnData.Length > 20)
                {
                    str = ReturnData.Substring(0x10, 2);
                }
            }
            catch
            {
                str = "";
            }
            return str;
        }
        public override string[,] GetAlarm()
        {
            string[,] strArray = new string[8, 11];
            try
            {
                Send("AA8001FF0000");
                int tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                string str = ReturnData.Trim();
                string str2 = str.Substring(0, 8);
                string str3 = str.Substring(8, 2);
                byte num2 = Convert.ToByte(str.Substring(10, 2), 0x10);
                string str4 = str.Substring(12, num2 * 2);
                if (!(str2 == "AA 00 01 FF") || (Convert.ToInt32(str3, 0x10) != CalcCRC(num2.ToString("X2") + str4)))
                {
                    return strArray;
                }
                for (int i = 0; i < 8; i++)
                {
                    byte num4 = Convert.ToByte(str4.Substring(i * 8, 2), 0x10);
                    byte num5 = Convert.ToByte(str4.Substring((i * 8) + 2, 2), 0x10);
                    byte num6 = Convert.ToByte(str4.Substring((i * 8) + 4, 2), 0x10);
                    strArray[i, 0] = "00";
                    strArray[i, 1] = ((num4 & 1) == 1) ? "1" : "0";
                    strArray[i, 2] = ((num4 & 2) == 2) ? "1" : "0";
                    strArray[i, 3] = ((num4 & 4) == 4) ? "1" : "0";
                    strArray[i, 4] = ((num4 & 8) == 8) ? "1" : "0";
                    strArray[i, 5] = ((num4 & 0x10) == 0x10) ? "1" : "0";
                    strArray[i, 6] = ((num4 & 0x20) == 0x20) ? "1" : "0";
                    strArray[i, 7] = ((num4 & 0x40) == 0x40) ? "1" : "0";
                    strArray[i, 8] = ((num4 & 0x80) == 0x80) ? "1" : "0";
                    strArray[i, 9] = num6.ToString();
                    strArray[i, 10] = num5.ToString();
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return strArray;
        }
        public override int GetFirmVersion()
        {
            var str = -1;
            try
            {
                Send("AA 80 03 FF 00 00");
                int tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < WaitDelay) && IsReciving)
                {
                    Application.DoEvents();
                }
                if (ReturnData.Trim().Substring(0, 12) == "AA0003FF0C0C")
                {
                   // str1 = "Iwon";
                }
                if (!(ReturnData.Trim() == "AA0000F003020100") && !(ReturnData.Trim() == "AA0000F002020101"))
                {
                    if (ReturnData.Trim().Trim() == "AA0000F000020200")
                    {
                        str = 02;
                    }
                 
                }
                return str;
            }
            catch
            {
                return str;
            }
        }
        public override PersonCfg GetPersonConfig()
        {
            PersonCfg cfg = new PersonCfg();
            try
            {
                Send("AA 80 02 FF 00 00");
                int tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                string str = ReturnData.Trim();
                string str2 = str.Substring(12, 2);
                cfg.weight = Convert.ToInt32(str2, 0x10);
                string str3 = str.Substring(14, 2);
                cfg.height = Convert.ToInt32(str3, 0x10);
                if (str.Substring(0x10, 2) == "00")
                {
                    cfg.sex = "man";
                }
                else
                {
                    cfg.sex = "woman";
                }
                string str5 = str.Substring(0x12, 2);
                cfg.age = Convert.ToInt32(str5, 0x10);
                string str6 = str.Substring(0x16, 2);
                string str7 = str.Substring(20, 2);
                cfg.goal = (Convert.ToInt32(str6, 0x10) * 0x100) + Convert.ToInt32(str7, 0x10);
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return cfg;
        }
        public string[,] GetOldAlarmSet()
        {
            string[,] strArray = new string[0x10, 8];
            try
            {
                Send("AA 80 01 F0 00 00");
                int tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                string inputString = ReturnData.Trim();
                string[] strArray2 = HexStr2HexArr(inputString);
                string[] strArray3 = HexByte2BiteArr(strArray2[6]);
                for (int i = 0; i < 8; i++)
                {
                    strArray[0, i] = strArray3[i];
                }
                strArray3 = HexByte2BiteArr(strArray2[7]);
                for (int j = 0; j < 8; j++)
                {
                    strArray[1, j] = strArray3[j];
                }
                strArray3 = HexByte2BiteArr(strArray2[8]);
                for (int k = 0; k < 8; k++)
                {
                    strArray[2, k] = strArray3[k];
                }
                strArray3 = HexByte2BiteArr(strArray2[9]);
                for (int m = 0; m < 8; m++)
                {
                    strArray[3, m] = strArray3[m];
                }
                strArray3 = HexByte2BiteArr(strArray2[10]);
                for (int n = 0; n < 8; n++)
                {
                    strArray[4, n] = strArray3[n];
                }
                strArray3 = HexByte2BiteArr(strArray2[11]);
                for (int num7 = 0; num7 < 8; num7++)
                {
                    strArray[5, num7] = strArray3[num7];
                }
                strArray3 = HexByte2BiteArr(strArray2[12]);
                for (int num8 = 0; num8 < 8; num8++)
                {
                    strArray[6, num8] = strArray3[num8];
                }
                strArray3 = HexByte2BiteArr(strArray2[13]);
                for (int num9 = 0; num9 < 8; num9++)
                {
                    strArray[7, num9] = strArray3[num9];
                }
                strArray3 = HexByte2BiteArr(strArray2[14]);
                for (int num10 = 0; num10 < 8; num10++)
                {
                    strArray[8, num10] = strArray3[num10];
                }
                strArray3 = HexByte2BiteArr(strArray2[15]);
                for (int num11 = 0; num11 < 8; num11++)
                {
                    strArray[9, num11] = strArray3[num11];
                }
                strArray3 = HexByte2BiteArr(strArray2[0x10]);
                for (int num12 = 0; num12 < 8; num12++)
                {
                    strArray[10, num12] = strArray3[num12];
                }
                strArray3 = HexByte2BiteArr(strArray2[0x11]);
                for (int num13 = 0; num13 < 8; num13++)
                {
                    strArray[11, num13] = strArray3[num13];
                }
                strArray3 = HexByte2BiteArr(strArray2[0x12]);
                for (int num14 = 0; num14 < 8; num14++)
                {
                    strArray[12, num14] = strArray3[num14];
                }
                strArray3 = HexByte2BiteArr(strArray2[0x13]);
                for (int num15 = 0; num15 < 8; num15++)
                {
                    strArray[13, num15] = strArray3[num15];
                }
                strArray3 = HexByte2BiteArr(strArray2[20]);
                for (int num16 = 0; num16 < 8; num16++)
                {
                    strArray[14, num16] = strArray3[num16];
                }
                strArray3 = HexByte2BiteArr(strArray2[0x15]);
                for (int num17 = 0; num17 < 8; num17++)
                {
                    strArray[15, num17] = strArray3[num17];
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return strArray;
        }
     /*
        private string[,] getSptdata()
        {
            string[,] strArray = new string[0xa8, 5];
            try
            {
                this.Send("AA 80 13 F0 00 00");
                int tickCount = Environment.TickCount;
                this.IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && this.IsReciving)
                {
                    Application.DoEvents();
                }
                int num2 = Convert.ToInt32(this.ReturnData.Trim().Substring(0x10, 2), 0x10);
                if ((num2 < 1) || (num2 > 250))
                {
                    num2 = 80;
                }
                this.Send("AA 81 14 F0 88 11 f5 00 77 1b 00 00 00 00 00 00 00 00 00 00 00 00 00");
                tickCount = Environment.TickCount;
                this.IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && this.IsReciving)
                {
                    Application.DoEvents();
                }
                for (int i = 0; i < 0x38; i++)
                {
                    this.Send("AA 80 14 F0 00 00");
                    tickCount = Environment.TickCount;
                    this.IsReciving = true;
                    while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && this.IsReciving)
                    {
                        Application.DoEvents();
                    }
                    string inputString = this.ReturnData.Trim();
                    if (inputString.Length >= 20)
                    {
                        string[] strArray2 = this.HexStr2HexArr(inputString);
                        string str3 = inputString.Substring(0x10, 4);
                        string str4 = this.Method16(str3);
                        string s = strArray2[7].Substring(0, 1);
                        string str6 = strArray2[7].Substring(1, 1);
                        if (int.Parse(str6) < 4)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                strArray[(j + (6 * int.Parse(str6))) + (0x18 * int.Parse(s)), 0] = str4;
                                strArray[(j + (6 * int.Parse(str6))) + (0x18 * int.Parse(s)), 1] = (j + (6 * int.Parse(str6))).ToString();
                                strArray[(j + (6 * int.Parse(str6))) + (0x18 * int.Parse(s)), 2] = (Convert.ToInt32(strArray2[10 + (2 * j)], 0x10) + (Convert.ToInt32(strArray2[11 + (2 * j)], 0x10) * 0x100)).ToString();
                            }
                        }
                        else
                        {
                            for (int k = 0; k < 6; k++)
                            {
                                strArray[(k + (6 * (int.Parse(str6) - 4))) + (0x18 * int.Parse(s)), 3] = (Convert.ToInt32(strArray2[10 + (2 * k)], 0x10) + (Convert.ToInt32(strArray2[11 + (2 * k)], 0x10) * 0x100)).ToString();
                                strArray[(k + (6 * (int.Parse(str6) - 4))) + (0x18 * int.Parse(s)), 4] = ((num2 * (Convert.ToInt32(strArray2[10 + (2 * k)], 0x10) + (Convert.ToInt32(strArray2[11 + (2 * k)], 0x10) * 0x100))) / 800).ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return strArray;
        }
      */ 
#endregion

 #region Set Function
        public override bool SetTime(string newDatetime)
        {
            var flag = false;
            try
            {
                var time = Convert.ToDateTime(newDatetime);
                var s = time.ToString("yy");
                var str2 = time.ToString("MM");
                var str3 = time.ToString("dd");
                var str4 = time.ToString("HH");
                var str5 = time.ToString("mm");
                var str6 = time.ToString("ss");
                s = int.Parse(s).ToString("X");
                s = (s.Length < 2) ? ("0" + s) : s;
                str2 = (int.Parse(str2) - 1).ToString("X");
                str2 = (str2.Length < 2) ? ("0" + str2) : str2;
                str3 = (int.Parse(str3) - 1).ToString("X");
                str3 = (str3.Length < 2) ? ("0" + str3) : str3;
                str4 = int.Parse(str4).ToString("X");
                str4 = (str4.Length < 2) ? ("0" + str4) : str4;
                str5 = int.Parse(str5).ToString("X");
                str5 = (str5.Length < 2) ? ("0" + str5) : str5;
                str6 = int.Parse(str6).ToString("X");
                str6 = (str6.Length < 2) ? ("0" + str6) : str6;
                var str7 = s + " " + str2 + " " + str3 + " " + str4 + " " + str5 + " " + str6;
                var hexStr = "AA 81 16 F0 " + PrepareHexStr_1(str7);
                Send(hexStr);
                var tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                if (ReturnData.Trim() == "AA0116F0000101")
                {
                    flag = true;
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return flag;
        }
        public override bool SetAlarm(string[,] arrInput)
        {
            var flag = false;
            try
            {
                var strArray = new string[8];
                var strArray2 = new string[8];
                var strArray3 = new string[8];
                for (var i = 0; i < 8; i++)
                {
                    var str = "";
                    for (var j = 1; j < 9; j++)
                    {
                        str = str + arrInput[i, j];
                    }
                    str = Convert.ToInt32(str, 2).ToString("X");
                    strArray[i] = (str.Length < 2) ? ("0" + str) : str;
                    var str2 = Convert.ToInt32(arrInput[i, 9]).ToString("X");
                    strArray2[i] = (str2.Length < 2) ? ("0" + str2) : str2;
                    var str3 = Convert.ToInt32(arrInput[i, 10]).ToString("X");
                    strArray3[i] = (str3.Length < 2) ? ("0" + str3) : str3;
                }
                var str4 = arrInput[0, 0] + " " + strArray[0] + " " + strArray2[0] + " " + strArray3[0] + " " + arrInput[1, 0] + " " + strArray[1] + " " + strArray2[1] + " " + strArray3[1] + " " + arrInput[2, 0] + " " + strArray[2] + " " + strArray2[2] + " " + strArray3[2] + " " + arrInput[3, 0] + " " + strArray[3] + " " + strArray2[3] + " " + strArray3[3];
                var hexStr = "AA 81 17 F0 " + PrepareHexStr_2(str4);
                Send(hexStr);
                var tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                if (ReturnData.Trim() != "AA0117F0000101")
                {
                    return false;
                }
                str4 = arrInput[4, 0] + " " + strArray[4] + " " + strArray2[4] + " " + strArray3[4] + " " + arrInput[5, 0] + " " + strArray[5] + " " + strArray2[5] + " " + strArray3[5] + " " + arrInput[6, 0] + " " + strArray[6] + " " + strArray2[6] + " " + strArray3[6] + " " + arrInput[7, 0] + " " + strArray[7] + " " + strArray2[7] + " " + strArray3[7];
                hexStr = "AA 81 17 F0 " + PrepareHexStr_3(str4);
                Send(hexStr);
                tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                if (ReturnData.Trim() == "AA0117F0000101")
                {
                    flag = true;
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return flag;
        }
        public override bool SetPersonConfig()
        {
            var flag = false;
            try
            {
                var str = CurrentInfo.d_height.ToString("X");
                str = (str.Length < 2) ? ("0" + str) : str;
                var str2 = CurrentInfo.d_weight.ToString("X");
                str2 = (str2.Length < 2) ? ("0" + str2) : str2;
                var str3 = "00";
                if (CurrentInfo.d_sex == "woman")
                {
                    str3 = "01";
                }
                var str4 = CurrentInfo.d_age.ToString("X");
                str4 = (str4.Length < 2) ? ("0" + str4) : str4;
                var str5 = "0000" + CurrentInfo.d_goal.ToString("X");
                str5 = str5.Substring(str5.Length - 4, 4);
                var str6 = str5.Substring(2, 2);
                var str7 = str5.Substring(0, 2);
                var str8 = str + " " + str2 + " " + str3 + " " + str4 + " " + str6 + " " + str7;
                var hexStr = "AA 81 13 F0 " + PrepareHexStr_1(str8);
                Send(hexStr);
                var tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                if (ReturnData.Trim() == "AA0113F0000101")
                {
                    flag = true;
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return flag;
        }
 #endregion
    }
}
