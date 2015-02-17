using System;
using System.Collections;
using System.IO.Ports;
using System.Runtime.InteropServices;

// this interface for divice implemantation
using System.Windows.Forms;
using GeekSync4Band.Manager;

namespace GeekSync4Band.Model
{
    internal abstract class IDevice
    {
        protected SerialPort VSerialPort = new SerialPort();

        public string MyPortName = "";
        protected bool IsReciving;
        private string _returnData = "";
        protected const int WaitDelay = 0x3e8;
        protected PersonCfg CurrentPersonCfg;
        public DBDevice CurrentInfo = new DBDevice();

        public string ReturnData
        {
            get { return _returnData; }
            set { _returnData = value; }
        }



        public string[,] getSptdata()
        {
            string[,] strArray = new string[0xa8, 5];
            try
            {
                Send("AA 80 13 F0 00 00");
                int tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                int num2 = Convert.ToInt32(ReturnData.Trim().Substring(0x10, 2), 0x10);
                if ((num2 < 1) || (num2 > 250))
                {
                    num2 = 80;
                }
                Send("AA 81 14 F0 88 11 f5 00 77 1b 00 00 00 00 00 00 00 00 00 00 00 00 00");
                tickCount = Environment.TickCount;
                IsReciving = true;
                while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                {
                    Application.DoEvents();
                }
                for (int i = 0; i < 0x38; i++)
                {
                    Send("AA 80 14 F0 00 00");
                    tickCount = Environment.TickCount;
                    IsReciving = true;
                    while (((Environment.TickCount - tickCount) < (2 * WaitDelay)) && IsReciving)
                    {
                        Application.DoEvents();
                    }
                    string inputString = ReturnData.Trim();
                    if (inputString.Length >= 20)
                    {
                        string[] strArray2 = HexStr2HexArr(inputString);
                        string str3 = inputString.Substring(0x10, 4);
                        string str4 = Method16(str3);
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
            catch //(Exception exception)
            {
              //  throw new Exception(exception.ToString());
            }
            return strArray;
        }

        protected string Method16(string Parameter14)
        {
            string str = "";
            try
            {
                int num = Convert.ToInt32(Parameter14.Substring(2, 2) + Parameter14.Substring(0, 2), 0x10);
                int num2 = num & 0x1f;
                string str3 = (1 + num2).ToString();
                int num3 = (num & 480) >> 5;
                string str4 = (1 + num3).ToString();
                string str5 = (((num & 0x7e00) >> 9) + 0x7d0).ToString();
                str = str5 + "-" + str4 + "-" + str3;
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return str;
        }

        public void Close()
        {
            try
            {
                VSerialPort.DataReceived -= new SerialDataReceivedEventHandler(OnReceiveFunc);
                int tickCount = Environment.TickCount;
                while (((Environment.TickCount - tickCount) < WaitDelay) && IsReciving)
                {
                    Application.DoEvents();
                }

                VSerialPort.Close();
                VSerialPort.Dispose();
            }
            catch
            {


            }

        }

        // you can override this method in implementation
        public virtual void Initialize(string comPort)
        {
            MyPortName = comPort;
            CurrentInfo.d_fw = GetFirmVersion();
            CurrentInfo.d_brand = GetBand();
            CurrentInfo.d_mac = GetBmac();
            CurrentPersonCfg = GetPersonConfig();
            CurrentInfo.d_age = CurrentPersonCfg.age;

            CurrentInfo.d_goal = CurrentPersonCfg.goal;
            CurrentInfo.d_height = CurrentPersonCfg.height;
            CurrentInfo.d_weight = CurrentPersonCfg.weight;

            CurrentInfo.d_sex = CurrentPersonCfg.sex;

        }

        public abstract string GetTime();

        public abstract bool SetTime(string newDatetime);

        public abstract string GetBand();
        //public abstract bool CheckBand();

        protected bool CheckBand()
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
        public abstract string GetBmac();

        public abstract int GetFirmVersion();

        public abstract PersonCfg GetPersonConfig();

        public abstract bool SetPersonConfig();

        public abstract string[,] GetAlarm();

        public abstract bool SetAlarm(string[,] arrInput);

        protected string[] HexStr2HexArr(string inputString)
        {
            string[] strArray;
            try
            {
                var str = inputString.Replace(" ", "");
                var list = new ArrayList();
                var num = str.Length / 2;
                for (var i = 0; i < num; i++)
                {
                    var str3 = str.Substring(2 * i, 2);
                    list.Add(str3);
                }
                strArray = (string[])list.ToArray(typeof(string));
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return strArray;
        }

        protected string[] HexByte2BiteArr(string hexByte)
        {
            var strArray = new[] { "0", "0", "0", "0", "0", "0", "0", "0" };
            try
            {
                var str = Convert.ToString(Convert.ToInt32(hexByte, 0x10), 2);
                for (var i = 0; i < str.Length; i++)
                {
                    strArray[7 - i] = str.Substring((str.Length - 1) - i, 1);
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return strArray;
        }

        protected void Send(string hexStr)
        {
            try
            {
                if (!VSerialPort.IsOpen)
                {
                    OpenPort(MyPortName);
                }
                var str = hexStr.Replace(" ", "");

                var count = str.Length / 2;
                var buffer = HexStr2ByteArr(hexStr);
                VSerialPort.Write(buffer, 0, count);
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
        }

        protected byte[] HexStr2ByteArr(string inputString)
        {
            byte[] buffer;
            try
            {
                var str = inputString.Replace(" ", "");
                var list = new ArrayList();
                var num = str.Length / 2;
                for (var i = 0; i < num; i++)
                {
                    byte num3;
                    var str2 = str.Substring(2 * i, 2);
                    try
                    {
                        num3 = Convert.ToByte(str2, 0x10);
                    }
                    catch
                    {
                        throw new Exception("Error:" + str2);
                    }
                    list.Add(num3);
                }
                buffer = (byte[])list.ToArray(typeof(byte));
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString());
            }
            return buffer;
        }

        private void OnReceiveFunc(object sender, SerialDataReceivedEventArgs e)
        {
            IsReciving = true;
            var bytesToRead = VSerialPort.BytesToRead;
            if (bytesToRead >= 1)
            {
                var buffer = new byte[bytesToRead];
                VSerialPort.Read(buffer, 0, bytesToRead);
                var str = "";
                for (var i = 0; i < buffer.Length; i++)
                {
                    str = str + string.Format("{0:X2}", buffer[i]);
                }
                ReturnData = str;
                IsReciving = false;
            }
        }

        private bool OpenPort(string portName)
        {
            try
            {
                VSerialPort.PortName = portName;
                VSerialPort.ReceivedBytesThreshold = 1;
                VSerialPort.BaudRate = 0x1c200;
                VSerialPort.DataBits = 8;
                VSerialPort.StopBits = StopBits.One;
                VSerialPort.Parity = Parity.None;
                VSerialPort.WriteTimeout = 0x3e8;
                VSerialPort.ReadTimeout = 0x3e8;
                VSerialPort.NewLine = "\r\n";
                VSerialPort.DataReceived += OnReceiveFunc;
                VSerialPort.Open();
                return VSerialPort.IsOpen;
            }
            catch
            {
                return false;
            }
        }
        /*
        public struct LedStat
        {
            public bool Enable;
            public bool RedOn;
            public bool GreenOn;
            public bool BlueOn;
        }
        */

        [StructLayout(LayoutKind.Sequential)]
        public struct PersonCfg
        {
            public int weight;
            public int height;
            public string sex;
            public int age;
            public int goal;
        }

    }


}
