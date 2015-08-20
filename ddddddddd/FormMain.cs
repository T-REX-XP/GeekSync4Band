using BBBNOVA.BNComboBox;
using GeekSync4Band.Device;
using GeekSync4Band.Manager;
using GeekSync4Band.Model;
using GeekSync4Band.Properties;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using USBClassLibrary;
using GeekSync4Band.ServiceReference1;

using System.ServiceModel;

namespace GeekSync4Band
{
    public partial class FormMain : Form
    {
        private IDevice _band;
        private USBClass USBPort;
        private List<USBClass.DeviceProperties> _listOfUsbDeviceProperties;
        private bool _myUsbDeviceConnected;
        private Control[,] arrAlarms;
        private DateTime deviceDateTime;  // время на устройстве в момент замера
        private DateTime hostDateTime;
        private Logger _logger = LogManager.GetCurrentClassLogger();
        private System.Windows.Forms.Timer updateTimeTimer;

        private enum TypeOfMessage
        {
            Connected, Disconnect, SyncEnd, TimeSyncEnd
        }

        public FormMain()
        {
            //USB Connection
            USBPort = new USBClass();
            InitializeComponent();
            /// run app minimazin in tray by default       
            //this.WindowState = FormWindowState.Minimized;
            //this.ShowInTaskbar = false;
            this.Hide();    

            _listOfUsbDeviceProperties = new List<USBClass.DeviceProperties>();
            USBPort.USBDeviceAttached += USBPort_USBDeviceAttached;
            USBPort.USBDeviceRemoved += USBPort_USBDeviceRemoved;
            USBPort.RegisterForDeviceChange(true, Handle);
            UsbTryMyDeviceConnection();
            // Объединяем в массив все компоненты для отображения будильников
            arrAlarms = new Control[,] {
                {cbAlarmEnable1, tbAlarmTime1, cbAlarm1Sun, cbAlarm1Mon, cbAlarm1Tue, cbAlarm1Wed, cbAlarm1Thu, cbAlarm1Fri, cbAlarm1Sat},
                {cbAlarmEnable2, tbAlarmTime2, cbAlarm2Sun, cbAlarm2Mon, cbAlarm2Tue, cbAlarm2Wed, cbAlarm2Thu, cbAlarm2Fri, cbAlarm2Sat},
                {cbAlarmEnable3, tbAlarmTime3, cbAlarm3Sun, cbAlarm3Mon, cbAlarm3Tue, cbAlarm3Wed, cbAlarm3Thu, cbAlarm3Fri, cbAlarm3Sat},
                {cbAlarmEnable4, tbAlarmTime4, cbAlarm4Sun, cbAlarm4Mon, cbAlarm4Tue, cbAlarm4Wed, cbAlarm4Thu, cbAlarm4Fri, cbAlarm4Sat},
                {cbAlarmEnable5, tbAlarmTime5, cbAlarm5Sun, cbAlarm5Mon, cbAlarm5Tue, cbAlarm5Wed, cbAlarm5Thu, cbAlarm5Fri, cbAlarm5Sat},
                {cbAlarmEnable6, tbAlarmTime6, cbAlarm6Sun, cbAlarm6Mon, cbAlarm6Tue, cbAlarm6Wed, cbAlarm6Thu, cbAlarm6Fri, cbAlarm6Sat},
                {cbAlarmEnable7, tbAlarmTime7, cbAlarm7Sun, cbAlarm7Mon, cbAlarm7Tue, cbAlarm7Wed, cbAlarm7Thu, cbAlarm7Fri, cbAlarm7Sat},
                {cbAlarmEnable8, tbAlarmTime8, cbAlarm8Sun, cbAlarm8Mon, cbAlarm8Tue, cbAlarm8Wed, cbAlarm8Thu, cbAlarm8Fri, cbAlarm8Sat}
            };

            updateTimeTimer = new System.Windows.Forms.Timer();
            updateTimeTimer.Interval = 500;
            updateTimeTimer.Enabled = true;
            updateTimeTimer.Tick += new EventHandler(this.OnTimeTimer);

        }

        /// <summary>
        ///     Обработчик события OnTick таймера, который обновляет время
        /// </summary>
        private void OnTimeTimer(object sender, EventArgs e)
        {
            //if (devTimeIsReaded && !tbTime.Focused)
            //{
            DateTime timeToShow = DateTime.Now - (hostDateTime - deviceDateTime);
            tbTime.Text = timeToShow.ToString("yyyy-MM-dd HH:mm:ss");
            //}
        }

        private void iTalk_Toggle1_ToggledChanged()
        {
            iTalk_GroupBox1.Enabled = useVidonnCloudSync.Toggled;
        }

  

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Remove", "Are you sure ??", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
            {
                var selectedDev = (DBDevice)listView1.SelectedItems[0].Tag;
                DbManager.Instance().RemoveDevice(selectedDev.d_mac);
                LoadDeviceList();
            }
        }

        #region USB
        /// <summary>
        /// Try to connect to the device.
        /// </summary>
        /// <returns>True if success, false otherwise</returns>
        private bool UsbTryMyDeviceConnection()
        {
            if (USBClass.GetUSBDevice(uint.Parse("0451", NumberStyles.AllowHexSpecifier), uint.Parse("16AA", NumberStyles.AllowHexSpecifier), ref _listOfUsbDeviceProperties, true))
            {
                _band = new Vidonn();
                Connect();
                return true;
            } //TODO: Fix Iwown VID & PID
            if (USBClass.GetUSBDevice(uint.Parse("0452", NumberStyles.AllowHexSpecifier), uint.Parse("16AA", NumberStyles.AllowHexSpecifier), ref _listOfUsbDeviceProperties, true))
            {
                _band = new Iwown();
                Connect();
                return true;
            }
            /*
              else
              {
                //  Disconnect();
                  return false;
              }
              */

            return false;
        }

        private void USBPort_USBDeviceAttached(object sender, USBClass.USBDeviceEventArgs e)
        {
            if (!_myUsbDeviceConnected)
            {
                if (UsbTryMyDeviceConnection())
                {
                    _myUsbDeviceConnected = true;
                }
            }
        }

        private void USBPort_USBDeviceRemoved(object sender, USBClass.USBDeviceEventArgs e)
        {
            if (!USBClass.GetUSBDevice(uint.Parse("0451", NumberStyles.AllowHexSpecifier), uint.Parse("16AA", NumberStyles.AllowHexSpecifier), ref _listOfUsbDeviceProperties, false))
            {
                //My Device is removed
                _myUsbDeviceConnected = false;
                Disconnect();
            }
        }

        protected override void WndProc(ref Message m)
        {
            bool IsHandled = false;
            USBPort.ProcessWindowsMessage(m.Msg, m.WParam, m.LParam, ref IsHandled);
            base.WndProc(ref m);
        }



        private void Connect()
        {
            _logger.Debug("Connect");
            Thread newThread = new Thread(Sync);
            _myUsbDeviceConnected = true;
            newThread.Start();
        }
       
        private void Disconnect()
        {
            _logger.Debug("Disconnect");
            ShowBalloonTip(TypeOfMessage.Disconnect);
            _band.Close();
            _myUsbDeviceConnected = false;
        }

        private void Sync()
        {
            _logger.Debug("Sync");
            _band.Initialize(_listOfUsbDeviceProperties[0].COMPort);
            ShowBalloonTip(TypeOfMessage.Connected);
            DbManager.Instance().ProcessDevice(ref _band.CurrentInfo);
            BindDeviceInfo();
            if (DbManager.Instance().SyncSteps(ref _band) == true)
            {
                ShowBalloonTip(TypeOfMessage.SyncEnd);
                ShowProgress();
                LoadTop7GoalStatistic();
            }
            if (Settings.Default.useTimeAutosync)
            {
                SyncTime();
            }
        }      

        private void SyncTime()
        {
            if (_myUsbDeviceConnected)
            {
                _logger.Debug("Sync time");
                if (!_band.SetTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
                {
                    MessageBox.Show("Can't synchronize date and time to Device", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    deviceDateTime = DateTime.Now;
                    hostDateTime = deviceDateTime;
                }
                ShowBalloonTip(TypeOfMessage.TimeSyncEnd);
            }
        }

        public void SetPersonalInfo()
        {
            if (_myUsbDeviceConnected)
            {
                _logger.Debug("Set Personal Info");
                _band.CurrentInfo.d_age = int.Parse(tbAge.Text);
                _band.CurrentInfo.d_weight = int.Parse(tbWeight.Text);
                _band.CurrentInfo.d_height = int.Parse(tbHeight.Text);
                _band.CurrentInfo.d_goal = int.Parse(tbGoal.Text);
                _band.CurrentInfo.d_sex = ((cbSex.SelectedIndex == 1) ? "False" : "True");
                _band.SetPersonConfig();
            }

        }

      
        #endregion


        void LoadFromSettings()
        {
            txtLogin.Text = Settings.Default.Login;
            txtPass.Text = Settings.Default.Pass;
            useVidonnCloudSync.Toggled = Settings.Default.useVidonnCloudSync;
            useTimeAutosync.Toggled = Settings.Default.useTimeAutosync;
        }

        void SaveToSettings()
        {
            Settings.Default.Login = txtLogin.ApexTB.Text;
            Settings.Default.Pass = txtPass.ApexTB.Text;
            Settings.Default.useVidonnCloudSync = useVidonnCloudSync.Toggled;
            Settings.Default.useTimeAutosync = useTimeAutosync.Toggled;
            Settings.Default.Save();
        }
        private void BindDeviceInfo()
        {
            _logger.Debug("Bind Device Info");
            try
            {
                if (_band != null)
                {
                    label4.Invoke((MethodInvoker)(() => label4.Text = Convert.ToString(_band.CurrentInfo.d_fw)));
                    label5.Invoke((MethodInvoker)(() => label5.Text = _band.CurrentInfo.d_brand));
                    label6.Invoke((MethodInvoker)(() => label6.Text = _band.CurrentInfo.d_mac));
                    label11.Invoke((MethodInvoker)(() => label11.Text = _band.CurrentInfo.d_goal.ToString(CultureInfo.InvariantCulture)));
                    toolStripStatusLabel2.Text = _listOfUsbDeviceProperties[0].COMPort;
                    label8.Invoke((MethodInvoker)(() => label8.Text = _band.GetTime()));
                    _band.Close();
                }
            }
            catch
            {
            }
        }
      
        private void LoadTop7GoalStatistic()
        {
            hBarChart1.Items.Clear();
            _logger.Debug("Load Top 7 Statistic");
            hBarChart1.Description.Text = "Week Goals";
            var goals = DbManager.Instance().GetGoalsForWeek();
            foreach (var goal in goals)
            {
                var color = new Color();
                if (goal.s_steps < 3000)
                    color = Color.Red;
                if (goal.s_steps > 3000 && goal.s_steps < 7000)
                    color = Color.Orange;
                if (goal.s_steps > 7000)
                    color = Color.Green;

                hBarChart1.Add((double)goal.s_steps, goal.s_date.ToString().Replace("0:00:00", ""), color);
            }
        }

        private void setAlarmEnable(int alarmNo, bool enabled)
        {
            for (int i = 1; i <= 8; i++)
            {
                arrAlarms[alarmNo, i].Enabled = enabled;
            }
        }

        private void ShowProgress()
        {
            _logger.Debug("Show Progress");
            try
            {
                if (_band != null)
                {
                    var currentVal = DbManager.Instance().GetStepsByDate(dateTimePicker1.Value, _band.CurrentInfo.d_mac);
                    long max = _band.CurrentInfo.d_goal;
                    float val = (float)currentVal / (float)max;
                    val = val * (float)100;
                    if (val > 100)
                    {
                        val = 100;
                    }
                    circleProgressBar1.Value = (int)val;
                }
            }
            catch { }
        }

        private void ShowBalloonTip(TypeOfMessage messageType)
        {
            _logger.Debug("Show Ballon Tip");
            switch (messageType)
            {
                case TypeOfMessage.Connected:
                    notifyIcon1.BalloonTipTitle = _band.CurrentInfo.d_brand + " Connected";
                    notifyIcon1.BalloonTipText = "Syncronization started.\nPlease don't disconnect the band!";
                    notifyIcon1.Text = "Syncronization";
                    break;

                case TypeOfMessage.SyncEnd:
                    notifyIcon1.BalloonTipTitle = " Sync finished";
                    notifyIcon1.BalloonTipText = Resources.msg_sync_end;
                    notifyIcon1.Text = Application.ProductName;

                    break;

                case TypeOfMessage.Disconnect:
                    notifyIcon1.BalloonTipTitle = label5.Text + " Disconnected";
                    notifyIcon1.BalloonTipText = "Device was disconnect! ";
                    break;
                case TypeOfMessage.TimeSyncEnd:
                    notifyIcon1.BalloonTipTitle = "Sync";
                    notifyIcon1.BalloonTipText = "Time was synced!";

                    break;
            }
            notifyIcon1.ShowBalloonTip(5000);
        }
        
        private void LoadDeviceStaistic()
        {
            _logger.Debug("Load Device Staistic");
            listView2.Items.Clear();
            if (_band != null)
            {
                foreach (var log in DbManager.Instance().GetListStepByMac(_band.CurrentInfo.d_mac))
                {
                    var item = new ListViewItem(Convert.ToString(log.s_date));
                    item.SubItems.Add(Convert.ToString(log.s_steps));
                    item.SubItems.Add(Convert.ToString(log.s_distance));
                    item.SubItems.Add(Convert.ToString(log.s_calories));
                    item.Tag = log;

                    listView2.Items.Add(item);
                }
            }

        }

        private void LoadDeviceList()
        {
            _logger.Debug("Load Device List");
            listView1.Items.Clear();
            comboBox1.Items.Clear();

            foreach (var device in DbManager.Instance().GetListDevices())
            {
                var item = new ListViewItem(device.d_brand);
                item.SubItems.Add(device.d_mac);
                item.Tag = device;
                item.SubItems.Add(Convert.ToString(device.d_fw));
                listView1.Items.Add(item);

                ComboboxItem cItem = new ComboboxItem();
                cItem.Text = device.d_brand + " " + device.d_mac;
                cItem.Value = device;
                comboBox1.Items.Add(cItem);
            }
        }

        #region Events
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadFromSettings();
            LoadDeviceList();
            LoadTop7GoalStatistic();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveToSettings();

            //if (e.CloseReason == CloseReason.UserClosing)
            //{
            //  //  mynotifyicon.Visible = true;
            //    this.Hide();
            //    e.Cancel = true;
            //}
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                //this.WindowState = FormWindowState.Minimized;
                //this.ShowInTaskbar = false;
                this.Hide();

            }
                
            //e.Cancel = true;
            //Hide();
        }

        private void iTalk_TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            if (iTalk_TabControl1.SelectedIndex == 0 && _band == null)
            {
                BindDeviceInfo();
            }
            if (iTalk_TabControl1.SelectedIndex == 1 || iTalk_TabControl1.SelectedIndex == 2)
            {

            }
            if (iTalk_TabControl1.SelectedIndex == 2)
            {
                LoadDeviceStaistic();
            }
        }

        private void btnSyncTime_Click(object sender, EventArgs e)
        {
            SyncTime();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var test = ((BNComboBox)sender).SelectedItem;
            DBDevice device = (DBDevice)((ComboboxItem)test).Value;
            if (_band == null) { _band = new Vidonn(); }
            _band.CurrentInfo = DbManager.Instance().GetDevice(device.d_mac);
            BindDeviceInfo();
            LoadDeviceStaistic();
        }

        private void btnWriteAlarms_Click(object sender, EventArgs e)
        {
            string[,] arrValues = new string[8, 11];

            for (int k = 0; k < 8; k++)
            {
                arrValues[k, 0] = "00";
                arrValues[k, 1] = ((CheckBox)arrAlarms[k, 0]).Checked == true ? "1" : "0";  // Enable
                arrValues[k, 2] = ((CheckBox)arrAlarms[k, 2]).Checked == true ? "1" : "0";  // San
                arrValues[k, 3] = ((CheckBox)arrAlarms[k, 3]).Checked == true ? "1" : "0";  // Mon
                arrValues[k, 4] = ((CheckBox)arrAlarms[k, 4]).Checked == true ? "1" : "0";  // Tue
                arrValues[k, 5] = ((CheckBox)arrAlarms[k, 5]).Checked == true ? "1" : "0";  // Wed
                arrValues[k, 6] = ((CheckBox)arrAlarms[k, 6]).Checked == true ? "1" : "0";  // Thu
                arrValues[k, 7] = ((CheckBox)arrAlarms[k, 7]).Checked == true ? "1" : "0";  // Fri
                arrValues[k, 8] = ((CheckBox)arrAlarms[k, 8]).Checked == true ? "1" : "0";  // Sat
                arrValues[k, 9] = ((MaskedTextBox)arrAlarms[k, 1]).Text.Substring(0, 2);    // Hour
                arrValues[k, 10] = ((MaskedTextBox)arrAlarms[k, 1]).Text.Substring(3, 2);   // Min
            }
            _band.SetAlarm(arrValues);

        }

        private void btnReadAlarms_Click(object sender, EventArgs e)
        {
            string[,] alarmSet = _band.GetAlarm();
            for (int i = 0; i < 8; i++)
            {
                // Включен ли будильник
                bool alarmEnableValue = alarmSet[i, 1] == "1" ? true : false;
                ((CheckBox)arrAlarms[i, 0]).Checked = alarmEnableValue;
                setAlarmEnable(i, alarmEnableValue);

                // Время будильника
                string hour = "00" + Convert.ToInt32(alarmSet[i, 9]);
                hour = hour.Substring(hour.Length - 2, 2);
                string min = "00" + Convert.ToInt32(alarmSet[i, 10]);
                min = min.Substring(min.Length - 2, 2);

                ((MaskedTextBox)arrAlarms[i, 1]).Text = hour + ":" + min;


                // Дни недели
                for (int j = 2; j <= 8; j++)
                {
                    bool alarmDayValue = alarmSet[i, j] == "1" ? true : false;
                    ((CheckBox)arrAlarms[i, j]).Checked = alarmDayValue;
                }
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SyncTime();
        }

        private void btnWritePersData_Click(object sender, EventArgs e)
        {
            SetPersonalInfo();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var device = (DBDevice)listView1.SelectedItems[0].Tag;
                if (_band == null) { _band = new Vidonn(); }
                _band.CurrentInfo = DbManager.Instance().GetDevice(device.d_mac);
                BindDeviceInfo();
                LoadDeviceStaistic();
            }

        }

        private void btnReadDateTime_Click(object sender, EventArgs e)
        {
            if (_myUsbDeviceConnected)
            {
                // Читаем время
                string devDateTime = _band.GetTime();

                if (devDateTime == "")
                {
                    MessageBox.Show("Can't read date and time from Device", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    deviceDateTime = Convert.ToDateTime(devDateTime);
                    //tbTime.Text = deviceDateTime.ToString();
                    //     hostDateTime = DateTime.Now;
                    //devTimeIsReaded = true;
                }
            }

        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            ShowProgress();
        }

        private void btnReadPersData_Click(object sender, EventArgs e)
        {
            if (_band.CurrentInfo != null)
            {
                tbAge.Text = _band.CurrentInfo.d_age.ToString(CultureInfo.InvariantCulture);
                tbGoal.Text = _band.CurrentInfo.d_goal.ToString(CultureInfo.InvariantCulture);
                tbWeight.Text = _band.CurrentInfo.d_weight.ToString(CultureInfo.InvariantCulture);
                tbHeight.Text = _band.CurrentInfo.d_height.ToString(CultureInfo.InvariantCulture);
                cbSex.SelectedIndex = ((_band.CurrentInfo.d_sex == "False") ? 0 : 1);
            }
        }

        private void contextMenuStrip2_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var selectedDev = (DBDevice)listView1.SelectedItems[0].Tag;
            Clipboard.SetText(selectedDev.d_mac);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void viewStatisticToolStripMenuItem_Click(object sender, EventArgs e)
        {
          //  ShowDialog();
          
            iTalk_TabControl1.SelectedTab=tabPage1;
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            Opacity = 100;
            Show();
           // this.Hide();

        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            ///*
            //WSDL
            //http://www.vidonn.com/webservice/service.asmx?WSDL
            //*/

            ///*
            //<? xml version = "1.0" encoding = "utf-8" ?>
            //< soap : Envelope xmlns: soap = "http://schemas.xmlsoap.org/soap/envelope/" xmlns: xsi = "http://www.w3.org/2001/XMLSchema-instance" xmlns: xsd = "http://www.w3.org/2001/XMLSchema" >
            //< soap:Body >< CheckLogin xmlns = "http://tempuri.org/" >< LoginID > T - REX - XP@ya.ru </ LoginID >
            //< Password >c#102612</Password><MobileIMEI>PCSYNC</MobileIMEI><verCode>VD2013</verCode></CheckLogin></soap:Body></soap:Envelope>
            //    */

            // Create the binding
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Name = "UserNameSoapBinding";
            binding.Security.Mode = BasicHttpSecurityMode.None;
            binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
            binding.ReceiveTimeout = new TimeSpan(0, 5, 0);
            binding.OpenTimeout = new TimeSpan(0, 5, 0);
            binding.CloseTimeout = new TimeSpan(0, 5, 0);
            binding.SendTimeout = new TimeSpan(0, 5, 0);

            EndpointAddress endpoint = new EndpointAddress("http://www.vidonn.com/webservice/service.asmx");
            ServiceSoap service = new ServiceSoapClient(binding, endpoint);
            if (string.IsNullOrEmpty(txtLogin.ApexTB.Text) || string.IsNullOrEmpty(txtPass.ApexTB.Text))
            {
                MessageBox.Show("Please Enter your account details");
            }
            else
            {
                var result = service.CheckLogin(txtLogin.ApexTB.Text, txtPass.ApexTB.Text, "PCSYNC", "VD2013");
                MessageBox.Show(result);

            }

            //SoapClient client = new SoapClient("http://www.vidonn.com/webservice/service.asmx?WSDL");
            //client.LoadWSDL("http://www.vidonn.com/webservice/service.asmx?WSDL",true);
            ////    foreach (var item in client.Wsdl.Operations)
            ////{
            ////    MessageBox.Show(item.Name);
            ////}
            //Soap.SoapHeader header = new SoapHeader();
            //SoapBuilder builder = new SoapBuilder();



        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var aboutBox = new Forms.formAbout())
            {
                aboutBox.ShowDialog();
            }
        }
        #endregion

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            iTalk_TabControl1.SelectedTab = tabPage4;
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            Opacity = 100;
            Show();
        }

    }
}
