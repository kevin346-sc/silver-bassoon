using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PCOMMSERVERLib;

namespace PMAC_C_SHARP_EXAMPLE
{
    public partial class DEMO : Form
    {

        public static PCOMMSERVERLib.PmacDeviceClass PMAC;
        public bool selectPmacSuccess = false;
        public bool openPmacSuccess = false;
        public int pmacNumber;
   

        // 1. （打开程序后的动作）
        public DEMO()
        {
            InitializeComponent();
            PMAC = new PmacDeviceClass();
        }

        // 2. 建立通讯
        private void selectDevice_Click(object sender, EventArgs e)
        {     
            PMAC.SelectDevice(0,out pmacNumber,out selectPmacSuccess);
            if ( selectPmacSuccess )
            {
                PMAC.Open(pmacNumber, out openPmacSuccess);
                if (openPmacSuccess)
                {
                    selectDevice.Text = "通讯成功";
                }
            }                    
        }

        // 3. 发送指令
        private void motor1jog_Click(object sender, EventArgs e)
        {
            string ans = null ;
            PMAC.GetResponse(pmacNumber, "#1j+", out ans);
        }

        private void stop_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#1j/", out ans);
        }

        // 4.读取变量
        private void read_Click(object sender, EventArgs e)
        {
            string pmacAnswer = null;
            string readVariable = null;
            int pmacStatus = 0;
            readVariable = textBox1.Text;
            PMAC.GetResponseEx(pmacNumber, readVariable, true, out pmacAnswer,out pmacStatus);
            //PMAC.GetResponse(pmacNumber, readVariable, out pmacAnwser);
            textBox1.Text = pmacAnswer;

        }

        // 5.下载文件
        private void download_Click(object sender, EventArgs e)
        {
            string fileDirectory = null;
            bool downloadSuccess = false;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "打开PMAC文件";
            ofd.Filter = "ASCII 文件 |  *.txt;*.pmc";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                fileDirectory = ofd.FileName;
                PMAC.Download(pmacNumber, fileDirectory, false, false, true, true, out downloadSuccess);
                toolStripStatusLabel2.Visible = true;
                if (downloadSuccess)
                {
                    toolStripStatusLabel2.Text = "下载成功";
                    download.Text = "下载成功";
                }
                else 
                    toolStripStatusLabel2.Text = "下载失败";
            }
        }

        // 6.控制显示状态
        private void timer1_Tick(object sender, EventArgs e)
        {
            if ( openPmacSuccess )
            {
                motor1jog.Enabled = true;
                stop.Enabled = true;
                read.Enabled = true;
                textBox1.Enabled = true;
                toolStripStatusLabel1.Text = "通讯正常";
                download.Enabled = true;
            }
            else
            {
                motor1jog.Enabled = false;
                stop.Enabled = false;
                read.Enabled = false;
                textBox1.Enabled = false;
                download.Enabled = false;
            } 
        }

        // 7.菜单功能
        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openPmacSuccess)
            {
                PMAC.Close(pmacNumber);
            }           
            openPmacSuccess = false;
            //状态栏功能
            toolStripStatusLabel1.Text = "已断开通讯";
            this.Close();
        }  
    }
}
