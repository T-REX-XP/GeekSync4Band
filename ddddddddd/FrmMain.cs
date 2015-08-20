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
       

      
        public FrmMain()
        {
            InitializeComponent();

           

        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

    


    
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
           
        }


       
        private void button2_Click(object sender, EventArgs e)
        {

        }

     

        private void button10_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
           
        }

     
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
           
        }

    

        private void button4_Click(object sender, EventArgs e)
        {
           
        }

        private void button6_Click(object sender, EventArgs e)
        {
           
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
           
        }

    
        private void neoTabWindow1_SelectedIndexChanged(object sender, NeoTabControlLibrary.SelectedIndexChangedEventArgs e)
        {
            //listView1.Items.Clear();
            //if (neoTabWindow1.SelectedIndex == 0 && _band == null)
            //{
            //    BindDeviceInfo();
            //}
            //if (neoTabWindow1.SelectedIndex == 1 || neoTabWindow1.SelectedIndex == 2)
            //{
            //    LoadDeviceList();
            //}
            //if (neoTabWindow1.SelectedIndex == 2)
            //{
            //    LoadDeviceStaistic();
            //}
        }

        private void viewStatisticToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {

        }
    }
}
