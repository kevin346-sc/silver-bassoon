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
    public partial class Main : Form
    {
        public static PCOMMSERVERLib.PmacDeviceClass PMAC;
        public bool selectPmacSuccess = true;
        public bool openPmacSuccess = false;
        public int pmacNumber, vol = 20;
        public string i,motor, mode;
        public double distance;
        public double P5, P10, P1, P2, P3, P4;//画圆公式
        public double Xxianwei1 = -15000, Xxianwei2 = 370000, Zxianwei1 = -10000, Zxianwei2 = 35000, Yxianwei1 = -25000, Yxianwei2 = 188500;


        //使打开程序后先显示建立通讯的界面
        public Main()
        {
            InitializeComponent();
            PMAC = new PmacDeviceClass();

        }


        //选择控制方式
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (Mode_Select.SelectedItem.ToString())
            {
                case "点动式": mode = "diandongshi"; break;
                case "增量式": mode = "zengliangshi"; break;
                case "绝对式": mode = "jueduishi"; break;
                case "直动式": mode = "zhidongshi"; break;
            }
        }

        //选择控制的电机
        private void Motor_Select_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Motor_Select.SelectedIndex = Motor_Select.Items.IndexOf("x");
            switch (Motor_Select.SelectedItem.ToString())
            {
                case "x": i = "1"; break;
                case "y": i = "2"; break;
                case "z": i = "3"; break;
                case "θ": i = "4"; break;
            }
        }

        // 控制电机点动式正向运动
        private void diandong1_Click(object sender, EventArgs e)
        {
            motor = '#' + i + "J^5000";
            string ans = null;
            PMAC.GetResponse(pmacNumber, motor, out ans);
        }

        //控制电机点动式反向运动
        private void diandong2_Click_1(object sender, EventArgs e)
        {
            motor = '#' + i + "J^-5000";
            string ans = null;
            PMAC.GetResponse(pmacNumber, motor, out ans);
        }

        //将输入的文本格式转变成数字
        private void submit_Click(object sender, EventArgs e)
        {
            string str = textBox2.Text.ToString();//储存输入的距离文本并进行格式转换
            try
            {
                if (int.Parse(textBox2.Text) > 0)
                {
                    distance = int.Parse(str) * 1683.4;
                    //电机运动距离
                }
                else
                {
                    MessageBox.Show("请输入正整数");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("请输入正整数");
            }
        }

        //控制电机增量式反向运动
        private void zengliang2_Click(object sender, EventArgs e)
        {
            string ans;
            motor = '#' + i + "J^-" + Convert.ToString(distance);
            MessageBox.Show("运动的距离为" + Convert.ToString(distance) + "ct");
            PMAC.GetResponse(pmacNumber, motor, out ans);
        }
        //控制电机增量式正向运动
        private void zengliang1_Click_1(object sender, EventArgs e)
        {
            string ans;
            motor = '#' + i + "J^" + Convert.ToString(distance);
            MessageBox.Show("运动的距离为" + Convert.ToString(distance) + "ct");
            PMAC.GetResponse(pmacNumber, motor, out ans);
        }

        //控制电机绝对式反向运动
        private void juedui2_Click(object sender, EventArgs e)
        {
            string ans;
            motor = '#' + i + "J=-" + Convert.ToString(distance);
            MessageBox.Show("运动的距离为" + Convert.ToString(distance) + "ct");
            PMAC.GetResponse(pmacNumber, motor, out ans);
        }
        //控制电机绝对式正向运动
        private void juedui1_Click(object sender, EventArgs e)
        {
            string ans;
            motor = '#' + i + "J=" + Convert.ToString(distance);
            MessageBox.Show("运动的距离为" + Convert.ToString(distance) + "ct");
            PMAC.GetResponse(pmacNumber, motor, out ans);
        }

        //控制电机直动式反向运动
        private void Zhidong2_Click(object sender, EventArgs e)
        {
            motor = '#' + i + "j-";
            string ans = null;
            PMAC.GetResponse(pmacNumber, motor, out ans);
        }

        //控制电机直动式正向运动
        private void Zhidong1_Click(object sender, EventArgs e)
        {
            motor = '#' + i + "j+";
            string ans = null;
            PMAC.GetResponse(pmacNumber, motor, out ans);
        }

        //回零按钮
        private void Home_Click(object sender, EventArgs e)
        {
            /*string ans = null;
            PMAC.GetResponse(pmacNumber, "#1J={0}", out ans);
            PMAC.GetResponse(pmacNumber, "#2J={0}", out ans);
            PMAC.GetResponse(pmacNumber, "#3J={0}", out ans);
            PMAC.GetResponse(pmacNumber, "#4J={0}", out ans);*/
            string fileDirectory, ans;
            bool downloadSuccess;
            string velosity1, velosity2, velosity3, velosity4;

            fileDirectory = @"C:\Users\汕头大学\Desktop\HMZ.txt";
            PMAC.Download(pmacNumber, fileDirectory, false, false, true, true, out downloadSuccess);
            PMAC.GetResponse(pmacNumber, "ENABLE PLC1,2,3,4", out ans);

            PMAC.GetResponse(pmacNumber, "#1v", out velosity1);
            PMAC.GetResponse(pmacNumber, "#2v", out velosity2);
            PMAC.GetResponse(pmacNumber, "#3v", out velosity3);
            PMAC.GetResponse(pmacNumber, "#4v", out velosity4);
            if (Convert.ToDouble(velosity1) == 0 && Convert.ToDouble(velosity2) == 0 && Convert.ToDouble(velosity3) == 0 && Convert.ToDouble(velosity4) == 0)
            {
                MessageBox.Show("回零完成！");
            }

        }

        //停止按钮
        private void stop_Click(object sender, EventArgs e)
        {
            string ans = null;
            motor = '#' + i + "j/";
            PMAC.GetResponse(pmacNumber, motor, out ans);
        }

        //速度调节按钮
        private void Slow_down_Click(object sender, EventArgs e)
        {
            int numD = 0;
            string ans, velocity;

            numD++;
            vol = vol - numD * 10;
            velocity = Convert.ToString(vol);
            PMAC.GetResponse(pmacNumber, "I122 = " + velocity, out ans);
            PMAC.GetResponse(pmacNumber, motor, out ans);
        }
        private void Speed_up_Click(object sender, EventArgs e)
        {
            int numU = 0;
            string ans, velocity;

            numU++;
            vol = vol + numU * 10;
            velocity = Convert.ToString(vol);
            PMAC.GetResponse(pmacNumber, "I122 =" + velocity, out ans);
            PMAC.GetResponse(pmacNumber, motor, out ans);
        }


        // 读取变量
        private void read_Click(object sender, EventArgs e)
        {
            string pmacAnswer = null;
            string readVariable = null, a = null;
            int pmacStatus = 0, click_num = 0;

            readVariable = textBox1.Text;
            PMAC.GetResponseEx(pmacNumber, readVariable, true, out pmacAnswer, out pmacStatus);
            //PMAC.GetResponse(pmacNumber, readVariable, out pmacAnwser);

           
            ++ click_num;
            Label[] bianliang = new Label[5];
            Label[] value = new Label[5];
            bianliang[0] = bianliang1;
            bianliang[1] = bianliang2;
            bianliang[2] = bianliang3;
            bianliang[3] = bianliang4;
            bianliang[4] = bianliang5;

            value[0] = value1;
            value[1] = value2;
            value[2] = value3;
            value[3] = value4;
            value[4] = value5;
            a = Convert.ToString(click_num);

            if (click_num <= 5)
            {               
                MessageBox.Show (a);
                bianliang[click_num - 1].Visible = true;
                bianliang[click_num - 1].Text = textBox1.Text;
                value[click_num - 1].Visible = true;
                value[click_num - 1].Text = pmacAnswer;

            }
            else
            {
                bianliang5.Text = textBox1.Text;
                value5.Text = pmacAnswer;
            }


            textBox1.Text = pmacAnswer;
        }

        // 下载文件
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
                textBox1.Text = ofd.FileName;
                PMAC.Download(pmacNumber, fileDirectory, false, false, true, true, out downloadSuccess);
                toolStripStatusLabel2.Visible = true;
                if (downloadSuccess)
                {
                    download.Text = "下载成功";
                }
                else
                    download.Text = "下载失败";
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //显示各个电机的位置速度信息
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#1P", out ans);
            textBox3.Text = ans;           
            while (Convert.ToDouble(ans) <= Xxianwei1 | Convert.ToDouble(ans) >= Xxianwei2)
            {
                MessageBox.Show("x轴已达限位，正在停机···");
                PMAC.GetResponse(pmacNumber, "#1J/", out ans);
            }
            PMAC.GetResponse(pmacNumber, "#1V", out ans);
            textBox4.Text = ans;


            PMAC.GetResponse(pmacNumber, "#2P", out ans);
            textBox5.Text = ans;
            while (Convert.ToDouble(ans) <= Yxianwei1 | Convert.ToDouble(ans) >= Yxianwei2)
            {
                MessageBox.Show("y轴已达限位，正在停机···");
                PMAC.GetResponse(pmacNumber, "#2J/", out ans);
            }
            PMAC.GetResponse(pmacNumber, "#2V", out ans);
            textBox6.Text = ans;


            PMAC.GetResponse(pmacNumber, "#3P", out ans);
            textBox7.Text = ans;
            while (Convert.ToDouble(ans) <= Zxianwei1 | Convert.ToDouble(ans) >= Zxianwei2)
            {
                MessageBox.Show("z轴已达限位，正在停机···");
                PMAC.GetResponse(pmacNumber, "#3J/", out ans);
            }
            PMAC.GetResponse(pmacNumber, "#3V", out ans);
            textBox8.Text = ans;

            PMAC.GetResponse(pmacNumber, "#4P", out ans);
            textBox9.Text = ans;
            PMAC.GetResponse(pmacNumber, "#4V", out ans);
            textBox10.Text = ans;
            


            //控制mode显示状态
            if (mode == "diandongshi")
            {
                diandong1.Visible = true;
                zengliang1.Visible = false;
                juedui1.Visible = false;
                zhidong1.Visible = false;
                diandong2.Visible = true;
                zengliang2.Visible = false;
                juedui2.Visible = false;
                zhidong2.Visible = false;
                label12.Visible = false;
                textBox2.Visible = false;
                label11.Visible = false;
                submit.Visible = false;
            }
            if (mode == "zengliangshi")
            {
                diandong1.Visible = false;
                zengliang1.Visible = true;
                juedui1.Visible = false;
                zhidong1.Visible = false;
                diandong2.Visible = false;
                zengliang2.Visible = true;
                juedui2.Visible = false;
                zhidong2.Visible = false;
                label12.Visible = true;
                textBox2.Visible = true;
                label11.Visible = true;
                submit.Visible = true;
            }

            if (mode == "jueduishi")
            {
                diandong1.Visible = false;
                zengliang1.Visible = false;
                juedui1.Visible = true;
                zhidong1.Visible = false;
                diandong2.Visible = false;
                zengliang2.Visible = false;
                juedui2.Visible = true;
                zhidong2.Visible = false;
                label12.Visible = true;
                textBox2.Visible = true;
                label11.Visible = true;
                submit.Visible = true;
            }

            if (mode == "zhidongshi")
            {
                diandong1.Visible = false;
                zengliang1.Visible = false;
                juedui1.Visible = false;
                zhidong1.Visible = true;
                diandong2.Visible = false;
                zengliang2.Visible = false;
                juedui2.Visible = false;
                zhidong2.Visible = true;
                label12.Visible = false;
                textBox2.Visible = false;
                label11.Visible = false;
                submit.Visible = false;
            }
        }

        private void mudi2_MouseEnter(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头镂空_左;
            ((Button)sender).Text = "后退";
            ((Button)sender).Font = new System.Drawing.Font("宋体", 16F);
        }
        private void mudi2_MouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头镂空_左__1_;
            ((Button)sender).Text = "";
        }
        private void mudi2_MouseUp(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头镂空_左__1_;
        }
        private void mudi2_MouseDown(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头镂空_左__2_;
        }

        private void mudi1_MouseDown(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头镂空_右_1_;
        }
        private void mudi1_MouseEnter(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头镂空_右;
            ((Button)sender).Text = "前进";
            ((Button)sender).Font = new System.Drawing.Font("宋体", 16F);
        }
        private void mudi1_MouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头镂空_右_2_;
            ((Button)sender).Text = "";
        }
        private void mudi1_MouseUp(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头镂空_右_2_;
        }

        private void download_MouseDown(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.下载;
        }
        private void download_MouseEnter(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.下载__1_;
            ((Button)sender).Text = "下载文件";
            ((Button)sender).Font = new System.Drawing.Font("宋体", 16F);
        }
        private void download_MouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.下载_2_;
            ((Button)sender).Text = "";
        }
        private void download_MouseUp(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.下载_2_;
        }

        private void stop_MouseDown(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.取消__2_;
        }
        private void stop_MouseEnter(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.取消__1_;
            ((Button)sender).Text = "停止";
            ((Button)sender).Font = new System.Drawing.Font("宋体", 16F);
        }
        private void stop_MouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.取消;
            ((Button)sender).Text = "";
        }
        private void stop_MouseUp(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.取消;
        }

        private void Home_MouseDown(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.门店及房型__2_;
        }
        private void Home_MouseEnter(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.门店及房型__1_;
            ((Button)sender).Text = "回零";
            ((Button)sender).Font = new System.Drawing.Font("宋体", 16F);
        }
        private void Home_MouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.门店及房型;
            ((Button)sender).Text = "";
        }
        private void Home_MouseUp(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.门店及房型;
        }

        private void speed_up_MouseEnter(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头_右;
            ((Button)sender).Text = "加速";
            ((Button)sender).Font = new System.Drawing.Font("宋体", 16F);
        }
        private void speed_up_MouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头_右__1_;
            ((Button)sender).Text = "";
        }
        private void speed_up_MouseUp(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头_右__1_;
        }
        private void speed_up_MouseDown(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头_右_2_;
        }

        private void slow_down_MouseDown(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头_左_1_;
        }
        private void slow_down_MouseEnter(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头_左;
            ((Button)sender).Text = "减速";
            ((Button)sender).Font = new System.Drawing.Font("宋体", 16F);
        }
        private void slow_down_MouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头_左_2_;
            ((Button)sender).Text = "";
        }
        private void slow_down_MouseUp(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.箭头_左_2_;
        }

        private void read_MouseDown(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.global_editing__2_;
        }
        private void read_MouseEnter(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.global_editing__1_;
            ((Button)sender).Text = "读取";
            ((Button)sender).Font = new System.Drawing.Font("宋体", 16F);
        }
        private void read_MouseUp(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.global_editing;
        }
        private void read_MouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.global_editing;
            ((Button)sender).Text = "";
        }

        private void plot_MouseDown(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.导入__2_;
        }
        private void plot_MouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.导入__1_;
            ((Button)sender).Text = "";
        }
        private void plot_MouseUp(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.导入__1_;
        }
        private void plot_MouseEnter(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.导入;
        }

        private void submit_MouseDown(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.重新提交;
        }
        private void submit_MouseEnter(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.重新提交__2_;
            queren.Visible = true;
        }
        private void submit_MouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.重新提交__1_;
            queren.Visible = false;
        }
        private void submit_MouseUp(object sender, MouseEventArgs e)
        {
            ((Button)sender).BackgroundImage = global::PMAC_C_SHARP_EXAMPLE.Properties.Resources.重新提交__1_;
        }

        private void Mode_Select_MouseEnter(object sender, EventArgs e)
        {
            yundongfangshi.Visible = true;
        }

        private void Motor_Select_MouseEnter(object sender, EventArgs e)
        {
            yundongdianji.Visible = true;
        }
        private void Mode_Select_MouseLeave(object sender, EventArgs e)
        {
            yundongfangshi.Visible = false;
        }
        private void Motor_Select_MouseLeave(object sender, EventArgs e)
        {
            yundongdianji.Visible = false;
        }

        //disable plc1，2，3，4指令
        private void Disable_Click(object sender, EventArgs e)
        {
            string ans;
            PMAC.GetResponse(pmacNumber, "DISABLE PLC0,1,2,3,4,6,7", out ans);
        }
        private void Disable_MouseEnter(object sender, EventArgs e)
        {
            disable.Visible = true;
        }
        private void Disable_MouseLeave(object sender, EventArgs e)
        {
            disable.Visible = false;
        }

        #region  画圆功能
        private void Circle_Click(object sender, EventArgs e)
        {
            string fileDirectory, answer;
            bool downloadSuccess;            
            if (!string.IsNullOrEmpty(Pfive.Text.ToString()) && !string.IsNullOrEmpty(Pten.Text.ToString()) && !string.IsNullOrEmpty(cos.Text.ToString()) && !string.IsNullOrEmpty(sin.Text.ToString()) && !string.IsNullOrEmpty(zuobiao1.Text.ToString()) && !string.IsNullOrEmpty(zuobiao2.Text.ToString()))
            {
                string ans;
                P5 = Convert.ToDouble(Pfive.Text.ToString());           
                P10 = Convert.ToDouble(Pten.Text.ToString());
                P1 = Convert.ToDouble(cos.Text.ToString());
                P2 = Convert.ToDouble(sin.Text.ToString());
                P3 = Convert.ToDouble(zuobiao1.Text.ToString());
                P4 = Convert.ToDouble(zuobiao2.Text.ToString());

                PMAC.GetResponse(pmacNumber, " P5 =" + P5.ToString(), out ans);
                PMAC.GetResponse(pmacNumber, " P10 =" + P10.ToString(), out ans);
                PMAC.GetResponse(pmacNumber, " P1 =" + P1.ToString(), out ans);
                PMAC.GetResponse(pmacNumber, " P2 =" + P2.ToString(), out ans);
                PMAC.GetResponse(pmacNumber, " P3 =" + P3.ToString(), out ans);
                PMAC.GetResponse(pmacNumber, " P4 =" + P4.ToString(), out ans);
            }
            else
            {
                MessageBox.Show("输入不能为空，请输入有效数字！");
            }

            fileDirectory = @"C:\Users\汕头大学\Desktop\圆弧.txt";
            PMAC.Download(pmacNumber, fileDirectory, false, false, true, true, out downloadSuccess);
            PMAC.GetResponse(pmacNumber, "&1 B4 R", out answer);
        }

        private void Plot_Click(object sender, EventArgs e)
        {
            string path;
            path = @"E:\Program Files (x86)\Delta Tau\PMAC Executive Pro2 Suite\PmacPlot32Pro2\PmacPlot32Pro2.exe";
            System.Diagnostics.Process.Start(path);
        }

        private void PID_Click(object sender, EventArgs e)
        {
            string path;
            path = @"E:\Program Files (x86)\Delta Tau\PMAC Executive Pro2 Suite\PmacTuningPro2\PmacTuningPro2.exe";
            System.Diagnostics.Process.Start(path);
        }
        #endregion

    }
}