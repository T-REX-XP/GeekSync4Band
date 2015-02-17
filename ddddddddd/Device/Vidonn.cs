
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GeekSync4Band.Model;

namespace GeekSync4Band.Device
{


    class Vidonn : IDevice
    {
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

        private string PrepareHexStr_1(string parameter8)
        {
            var str = CalcCRC_One(HexStr2ByteArr("F5 " + parameter8));
            var inputString = "08 F5 " + parameter8 + " " + str;
            return (CalcCRC_Two(HexStr2ByteArr(inputString)) + " " + inputString);

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
        /*
        public override bool CheckBand()
        {
            var flag = false;
            try
            {
                Send("AA 80 10 F0 00 00");
                var tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < WaitDelay) && IsReciving)
                {
                    Application.DoEvents();
                }
                if ((ReturnData.Length > 30) && (ReturnData.Substring(14, 12) == "5669646F6E6E"))
                {
                    flag = true;
                }
            }
            catch
            {
                flag = false;
            }
            return flag;
        }
        */
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

        public override int GetFirmVersion()
        {
            var str = -1;
            try
            {
                Send("AA 80 00 F0 00 00");
                var tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < WaitDelay) && IsReciving)
                {
                    Application.DoEvents();
                }
                if (ReturnData.Trim() != "AA0000F003020100" && ReturnData.Trim() != "AA0000F002020101")
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
            var cfg = new PersonCfg();
            try
            {
                Send("AA 80 13 F0 00 00");
                var tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                var str = ReturnData.Trim();
                var str2 = str.Substring(14, 2);
                cfg.height = Convert.ToInt32(str2, 0x10);
                var str3 = str.Substring(0x10, 2);
                cfg.weight = Convert.ToInt32(str3, 0x10);
                cfg.sex =Convert.ToString( str.Substring(0x12, 2) != "00");
                var str5 = str.Substring(20, 2);
                cfg.age = Convert.ToInt32(str5, 0x10);
                var str6 = str.Substring(0x18, 2);
                var str7 = str.Substring(0x16, 2);
                cfg.goal = (Convert.ToInt32(str6, 0x10) * 0x100) + Convert.ToInt32(str7, 0x10);
            }
            catch //(Exception exception)
            {
              //  throw new Exception(exception.ToString());
            }
            return cfg;
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
                if (CurrentInfo.d_sex=="False")
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

        public override string[,] GetAlarm()
        {
            var strArray = new string[8, 11];
            try
            {
                for (var i = 0; i < 2; i++)
                {
                    Send("AA 80 17 F0 00 00");
                    var tickCount = Environment.TickCount;
                    IsReciving = true;
                    while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                    {
                        Application.DoEvents();
                    }
                    var inputString = ReturnData.Trim();
                    if (inputString.Length >= 20)
                    {
                        var strArray2 = HexStr2HexArr(inputString);
                        if (strArray2[7] == "00")
                        {
                            strArray[0, 0] = strArray2[8];
                            var strArray3 = HexByte2BiteArr(strArray2[9]);
                            for (var j = 0; j < 8; j++)
                            {
                                strArray[0, j + 1] = strArray3[j];
                            }
                            strArray[0, 9] = Convert.ToInt32(strArray2[10], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[0, 10] = Convert.ToInt32(strArray2[11], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[1, 0] = strArray2[12];
                            strArray3 = HexByte2BiteArr(strArray2[13]);
                            for (var k = 0; k < 8; k++)
                            {
                                strArray[1, k + 1] = strArray3[k];
                            }
                            strArray[1, 9] = Convert.ToInt32(strArray2[14], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[1, 10] = Convert.ToInt32(strArray2[15], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[2, 0] = strArray2[0x10];
                            strArray3 = HexByte2BiteArr(strArray2[0x11]);
                            for (var m = 0; m < 8; m++)
                            {
                                strArray[2, m + 1] = strArray3[m];
                            }
                            strArray[2, 9] = Convert.ToInt32(strArray2[0x12], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[2, 10] = Convert.ToInt32(strArray2[0x13], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[3, 0] = strArray2[20];
                            strArray3 = HexByte2BiteArr(strArray2[0x15]);
                            for (var n = 0; n < 8; n++)
                            {
                                strArray[3, n + 1] = strArray3[n];
                            }
                            strArray[3, 9] = Convert.ToInt32(strArray2[0x16], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[3, 10] = Convert.ToInt32(strArray2[0x17], 0x10).ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            strArray[4, 0] = strArray2[8];
                            var strArray4 = HexByte2BiteArr(strArray2[9]);
                            for (var num7 = 0; num7 < 8; num7++)
                            {
                                strArray[4, num7 + 1] = strArray4[num7];
                            }
                            strArray[4, 9] = Convert.ToInt32(strArray2[10], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[4, 10] = Convert.ToInt32(strArray2[11], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[5, 0] = strArray2[12];
                            strArray4 = HexByte2BiteArr(strArray2[13]);
                            for (var num8 = 0; num8 < 8; num8++)
                            {
                                strArray[5, num8 + 1] = strArray4[num8];
                            }
                            strArray[5, 9] = Convert.ToInt32(strArray2[14], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[5, 10] = Convert.ToInt32(strArray2[15], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[6, 0] = strArray2[0x10];
                            strArray4 = HexByte2BiteArr(strArray2[0x11]);
                            for (var num9 = 0; num9 < 8; num9++)
                            {
                                strArray[6, num9 + 1] = strArray4[num9];
                            }
                            strArray[6, 9] = Convert.ToInt32(strArray2[0x12], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[6, 10] = Convert.ToInt32(strArray2[0x13], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[7, 0] = strArray2[20];
                            strArray4 = HexByte2BiteArr(strArray2[0x15]);
                            for (var num10 = 0; num10 < 8; num10++)
                            {
                                strArray[7, num10 + 1] = strArray4[num10];
                            }
                            strArray[7, 9] = Convert.ToInt32(strArray2[0x16], 0x10).ToString(CultureInfo.InvariantCulture);
                            strArray[7, 10] = Convert.ToInt32(strArray2[0x17], 0x10).ToString(CultureInfo.InvariantCulture);
                        }
                    }
                }
            }
            catch //(Exception exception)
            {
              //  throw new Exception(exception.ToString());
            }
            return strArray;
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

        private string PrepareHexStr_3(string parameter10)
        {
            var str = CalcCRC_One(HexStr2ByteArr("F5 01 " + parameter10));
            var inputString = "13 F5 01 " + parameter10 + " " + str;
            return (CalcCRC_Two(HexStr2ByteArr(inputString)) + " " + inputString);

        }

        private string PrepareHexStr_2(string parameter9)
        {
            var str = CalcCRC_One(HexStr2ByteArr("F5 00 " + parameter9));
            var inputString = "13 F5 00 " + parameter9 + " " + str;
            return (CalcCRC_Two(HexStr2ByteArr(inputString)) + " " + inputString);

        }
        public bool IsOpen
        {
            get
            {
                return VSerialPort.IsOpen;
            }
        }
    }
}
