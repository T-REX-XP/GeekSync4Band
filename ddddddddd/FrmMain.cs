using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using BBBNOVA.BNComboBox;
using GeekSync4Band.Device;
using GeekSync4Band.Manager;
using GeekSync4Band.Model;
using GeekSync4Band.Properties;
using USBClassLibrary;
using Application = System.Windows.Forms.Application;
using NLog;


namespace GeekSync4Band
{
    public partial class FrmMain : Form
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();
        private enum TypeOfMessage
        {
            Connected, Disconnect, SyncEnd, TimeSyncEnd
        }

        private IDevice _band;
        private USBClass USBPort;
        private List<USBClass.DeviceProperties> _listOfUsbDeviceProperties;
        private bool _myUsbDeviceConnected;
        private Control[,] arrAlarms;
        private DateTime deviceDateTime;  // время на устройстве в момент замера
        private DateTime hostDateTime;
        public FrmMain()
        {
            InitializeComponent();

            //USB Connection
            USBPort = new USBClass();
            _listOfUsbDeviceProperties = new List<USBClass.DeviceProperties>();
            USBPort.USBDeviceAttached += USBPort_USBDeviceAttached;
            USBPort.USBDeviceRemoved += USBPort_USBDeviceRemoved;
            USBPort.RegisterForDeviceChange(true, Handle);
            UsbTryMyDeviceConnection();
            _myUsbDeviceConnected = false;
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

        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
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
            newThread.Start();
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
        }

        void ShowProgress()
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
                    //   label8.Invoke((MethodInvoker)(() => label8.Text = _band.GetTime()));
                    //   circleProgressBar1.Invoke((MethodInvoker)(() => circleProgressBar1.Value = Convert.ToInt32((currentVal * 100) % max)));
                    //circleProgressBar1.Value = ;
                    circleProgressBar1.Value = (int)val;
                }

            }
            catch
            {


            }

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

        private void Disconnect()
        {
            _logger.Debug("Disconnect");
            ShowBalloonTip(TypeOfMessage.Disconnect);
            //  DbManager.Instance() = null;
            _band.Close();
            _band = null;
            // MessageBox.Show("Disconnected!");
        }
        #endregion

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

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            if (neoTabWindow1.SelectedIndex == 0 && _band == null)
            {
                BindDeviceInfo();
            }
            if (neoTabWindow1.SelectedIndex == 1 || neoTabWindow1.SelectedIndex == 2)
            {

            }
            if (neoTabWindow1.SelectedIndex == 2)
            {
                LoadDeviceStaistic();
            }
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
        private void button2_Click(object sender, EventArgs e)
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

        private void setAlarmEnable(int alarmNo, bool enabled)
        {
            for (int i = 1; i <= 8; i++)
            {
                arrAlarms[alarmNo, i].Enabled = enabled;
            }
        }

        private void button10_Click(object sender, EventArgs e)
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
            //  sPLib.Close();           

        }

        private void button3_Click(object sender, EventArgs e)
        {
            SetPersonalInfo();
        }

        public void SetPersonalInfo()
        {
            _logger.Debug("Set Personal Info");
            _band.CurrentInfo.d_age = int.Parse(tbAge.Text);
            _band.CurrentInfo.d_weight = int.Parse(tbWeight.Text);
            _band.CurrentInfo.d_height = int.Parse(tbHeight.Text);
            _band.CurrentInfo.d_goal = int.Parse(tbGoal.Text);
            _band.CurrentInfo.d_sex = ((cbSex.SelectedIndex == 1) ? "False" : "True");
            _band.SetPersonConfig();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SyncTime();
        }

        private void SyncTime()
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

        private void button4_Click(object sender, EventArgs e)
        {
            SyncTime();
        }

        private void button6_Click(object sender, EventArgs e)
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
                hostDateTime = DateTime.Now;
                //devTimeIsReaded = true;
            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            ShowProgress();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            LoadDeviceList();
            neoTabWindow1.Renderer = NeoTabControlLibrary.AddInRendererManager.LoadRenderer("CCleanerRendererVS3");
            neoTabWindow2.Renderer = NeoTabControlLibrary.AddInRendererManager.LoadRenderer("CCleanerRendererVS4");
            LoadTop7GoalStatistic();
        }

        private void LoadTop7GoalStatistic()
        {
            _logger.Debug("Load Top 7 Statictic");
            hBarChart1.Description.Text = "Week Goals";


            foreach (var goal in DbManager.Instance().GetGoalsForWeek())
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

        private void neoTabWindow1_SelectedIndexChanged(object sender, NeoTabControlLibrary.SelectedIndexChangedEventArgs e)
        {
            listView1.Items.Clear();
            if (neoTabWindow1.SelectedIndex == 0 && _band == null)
            {
                BindDeviceInfo();
            }
            if (neoTabWindow1.SelectedIndex == 1 || neoTabWindow1.SelectedIndex == 2)
            {
                LoadDeviceList();
            }
            if (neoTabWindow1.SelectedIndex == 2)
            {
                LoadDeviceStaistic();
            }
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

        private void contextMenuStrip2_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var selectedDev = (DBDevice)listView1.SelectedItems[0].Tag;
            Clipboard.SetText(selectedDev.d_mac);
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




    }
}
