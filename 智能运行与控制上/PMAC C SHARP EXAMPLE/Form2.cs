using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenCvSharp;
using PCOMMSERVERLib;


namespace PMAC_C_SHARP_EXAMPLE
{
    public partial class index : Form
    {
        public static PCOMMSERVERLib.PmacDeviceClass PMAC;
        public bool selectPmacSuccess = true;
        public bool openPmacSuccess = false;
        public int pmacNumber;

        private System.Drawing.Point p1, p2;//定义两个点（启点，终点）  
        private static bool drawing = false;//设置一个启动标志  

        int Diyix = 0, Diyiy = 0;
        int Dierx = 0, Diery = 0;

        //初始化第一个界面，显示建立通讯界面
        public index()
        {
            InitializeComponent();
            PMAC = new PmacDeviceClass();
        }


        private void selectDevice_Click_1(object sender, EventArgs e)
        {
            PMAC.SelectDevice(0, out pmacNumber, out selectPmacSuccess);
            selectDevice.Text = Convert.ToString(selectPmacSuccess);
            if (selectPmacSuccess)
            {
                PMAC.Open(pmacNumber, out openPmacSuccess);
                if (openPmacSuccess)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Dispose();
                    this.Close();
                    selectDevice.Text = "通讯成功";
                }
            }
            this.DialogResult = DialogResult.OK;
            this.Dispose();
        }

        private void index_Load(object sender, EventArgs e)
        {
            this.Controls.Add(selectDevice);

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (openPmacSuccess)
            {
                toolStripLabel1.Text = "通讯成功";
            }
            else
            {
                toolStripLabel1.Text = "通讯失败";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            //选择图片
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.RestoreDirectory = true;
            string imgName = "";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                imgName = openFileDialog.FileName;
            }
            Console.WriteLine("文件名为" + imgName);

            //读取并进行边缘检测
            //Mat dstImg = new Mat(srcImg, new Rect(x, y, width, height));
            Mat srcImg = new Mat(imgName, ImreadModes.Color);
            Cv2.ImShow("input", srcImg);

            Mat dstImg = new Mat();
            Cv2.Canny(srcImg, dstImg, 50, 200);
            Cv2.ImShow("output", dstImg);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked ==true)
            {
                button1.Text = "abc";
            }
            if (radioButton2.Checked==true)
            {
                button1.Text = "d";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //匹配图         
            Bitmap bmp1 = new Bitmap(pictureBox2.Image);
            Mat temp = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp1);//bitmap转 mat

            //被匹配图
            Bitmap bmp2 = new Bitmap(pictureBox1.Image);
            Mat wafer = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp2);//bitmap转 mat
            //匹配结果
            Mat result = new Mat();
            //模板匹配
            Cv2.MatchTemplate(wafer, temp, result, TemplateMatchModes.CCoeffNormed);//最好匹配为1,值越小匹配越差
            Double minVul;
            Double maxVul;
            OpenCvSharp.Point minLoc = new OpenCvSharp.Point(0, 0);
            OpenCvSharp.Point maxLoc = new OpenCvSharp.Point(0, 0);
            OpenCvSharp.Point matchLoc = new OpenCvSharp.Point(0, 0);
            Cv2.Normalize(result, result, 0, 1, NormTypes.MinMax, -1);//归一化
            Cv2.MinMaxLoc(result, out minVul, out maxVul, out minLoc, out maxLoc);//查找极值
            matchLoc = maxLoc;//最大值坐标
            //result.Set(matchLoc.Y, matchLoc.X, 0);//改变最大值为最小值  
            Mat mask = wafer.Clone();//复制整个矩阵
            //画框显示
            Cv2.Rectangle(mask, matchLoc, new OpenCvSharp.Point(matchLoc.X + temp.Cols, matchLoc.Y + temp.Rows), Scalar.Green, 2);
            //循环查找画框显示
            //Mat maskMulti = wafer.Clone();//复制整个矩阵
            for (int i = 1; i < result.Rows - temp.Rows; i += temp.Rows)//行遍历
            {
                for (int j = 1; j < result.Cols - temp.Cols; j += temp.Cols)//列遍历
                {
                    Rect roi = new Rect(j, i, temp.Cols, temp.Rows);        //建立感兴趣
                    Mat RoiResult = new Mat(result, roi);
                    Cv2.MinMaxLoc(RoiResult, out minVul, out maxVul, out minLoc, out maxLoc);//查找极值
                    matchLoc = maxLoc;//最大值坐标
                    if (maxVul > 0.8)
                    {
                        //创建画板
                        Graphics g = pictureBox1.CreateGraphics();
                        //画出识别的矩形
                        g.DrawRectangle(new Pen(Color.Yellow), j + maxLoc.X, i + maxLoc.Y, Dierx - Diyix, Diery - Diyiy);
                        string axis = '(' + Convert.ToString(i + maxLoc.Y) + ',' + Convert.ToString(j + maxLoc.X) + ')';
                        //写出坐标
                        g.DrawString(axis, this.Font, Brushes.Yellow, j + maxLoc.X, i + maxLoc.Y - 10);
                        g.Dispose();
                    }
                }
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Pen pen = new Pen(Color.Green, 2);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;     //绘制线的格式
            if (drawing)
            {
                e.Graphics.DrawRectangle(pen, Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
            }
            pen.Dispose();
        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            float a;
            a = trackBar1.Value / 100f;
            yuzhidaxiao.Text = "阈值大小：" + a.ToString();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Left)
            {
                p1 = e.Location;
                drawing= true;
            }

        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                p2 = e.Location;
                drawing = false;
            }

        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {

            if (drawing)
            {
                if (e.Button != MouseButtons.Left) return;
                p2 = e.Location;
                pictureBox1.Invalidate();
            }
        }  
    }
}
