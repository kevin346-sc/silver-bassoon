using System;
using System.Drawing;
using System.Windows.Forms;
using PCOMMSERVERLib;
using OpenCvSharp;

/// <summary>
//需保证每次开启前相机已关闭或程序正常终止（相机关闭状态）
//防止连续点击连拍后报错，可按暂停终止连拍
//防止连续点击连接相机后报错
//关闭相机之后显示不显示之前的照片
//改变photo_box中选点方式
//建立pmac通讯以及加上pmac控制
//选取的模板可从本地加载或保存
//完成标定坐标转换
//添加用户选择不同坐标显示
//选框实时显示
//阈值由用户动态选择
//用户点击识别到的芯片，自动将其移动到屏幕中心(timer实现)

//检测和匹配存在bug：连拍状态下无法取消显示前一次匹配结果——》只用单拍进行匹配
//不同坐标显示转换不能实时更新——》需要选择后再次点击图像识别按钮
/// </summary>

namespace jiemian2
{
    public partial class Form1 : Form
    {
        public static PCOMMSERVERLib.PmacDeviceClass PMAC;
        public int pmacNumber;
        public bool selectPmacSuccess = true;
        public bool openPmacSuccess = false;
        public double zuo = 0;
        public double you = 0;
        public double shang = 0;
        public double xia = 0;
        public double xxx = 250;//像素标定值
        public double yyy = 200;
        public double kx = 0;
        public double ky = 0;
        public float kx1 = 0;
        public float ky2 = 0;
        double tempx, tempy;//设置临时坐标检验是否在框内
        double positionx, positiony;
        public string ans1 = null;
        public string ans2 = null;

        private System.Drawing.Point p1, p2;//定义两个点（启点，终点）  
        private static bool drawing = false;//设置一个启动标志判断是否开始画框  
        public bool flag = false;//设置启动标志判断是否开启选框
        public bool zhangzifeng = false;//设置启动标志判断是否开启连拍
        public bool liuyifei = false;//设置启动标志判断是否开启选取芯片移动
        public bool xiaoshimei = false;//设置启动标志判断是否开启选取芯片移动

        public Bitmap bitmap1;
        public Bitmap newbitmap1;
        public Bitmap bitmap2;
        public Bitmap newbitmap2;


        Double threshold = 0.8;//默认模板匹配阈值为0.8
        Class1 camera = new Class1();

        //初始加载界面
        public Form1()
        {
            InitializeComponent();
            PMAC = new PmacDeviceClass();
            camera.CameraImageEvent += Camera_CameraImageEvent;
            Unanble();
            panel1.Visible = false;
        }

        /*相机操作*/
        private void Connect_cam_Click(object sender, EventArgs e)
        {
            if (camera.CameraNumber > 0)
            {
                camera.CameraInit();
                MessageBox.Show("已成功连接");
                pause.Visible = true;
                oneshot.Visible = true;
                keepshot.Visible = true;
                connect_cam.Visible = false;
                shut_cam.Visible = true;
            }
            else
            {
                MessageBox.Show("未连接到相机,请检查接线再重试");
                Unanble();
            }
        }
        private void Camera_CameraImageEvent(Bitmap bmp)
        {
            photo_box.Invoke(new MethodInvoker(delegate
            {
                Bitmap old = photo_box.Image as Bitmap;
                photo_box.Image = bmp;
                if (old != null)
                    old.Dispose();
            }));
        }
        void Unanble()
        {
            shut_cam.Visible = false;
            pause.Visible = false;
            oneshot.Visible = false;
            keepshot.Visible = false;
            save_bmp.Visible = false;
            label8.Visible = false;
            label9.Visible = false;
            label10.Visible = false;
            label11.Visible = false;
            label12.Visible = false;
            label7.Visible = false;
            end.Visible = false;
            confirm.Visible = false;
            panel2.Visible = false;
            panel3.Visible = false;
            xinpianxuanze.Visible = false;
        }
        private void Shut_cam_Click(object sender, EventArgs e)
        {
            string answer = null;
            camera.DestroyCamera();
            Unanble();
            connect_cam.Visible = true;
            photo_box.Image = null;
            PMAC.GetResponse(pmacNumber, "M514 = 0", out answer);
            label8.Visible = false;
            label9.Visible = false;
            label10.Visible = false;
            label11.Visible = false;
            label12.Visible = false;
            label7.Visible = false;
        }
        private void Keepshot_Click_1(object sender, EventArgs e)
        {
            zhangzifeng = true;
            pause.Visible = true;
            //string answer = null;
            //camera.KeepShot();
            //oneshot.Visible = false;
            //keepshot.Visible = false;
            //PMAC.GetResponse(pmacNumber, "M514 = 1", out answer);
        }
        private void Oneshot_Click_1(object sender, EventArgs e)
        {
            string answer = null;
            pause.Visible = false;
            camera.OneShot();
            PMAC.GetResponse(pmacNumber, "M514 = 1", out answer);
        }
        private void Pause_Click_1(object sender, EventArgs e)
        {
            zhangzifeng = false;
            xiaoshimei = false;
            liuyifei = false;
            camera.Stop();
            oneshot.Visible = true;
            keepshot.Visible = true;
        }

        /*在图上选取模板和显示模板*/
        private void photo_box_MouseDown(object sender, MouseEventArgs e)
        {
            if (flag == true)
            {
                if (e.Button == MouseButtons.Left)
                {
                    p1 = e.Location;
                    drawing = true;
                }
            }
        }
        private void photo_box_MouseMove(object sender, MouseEventArgs e)
        {
            Graphics g = photo_box.CreateGraphics();
            if (flag == true)
            {
                if (drawing)
                {
                    if (e.Button != MouseButtons.Left) return;
                    p2 = e.Location;
                    photo_box.Invalidate();
                }
            }
        }
        private void photo_box_MouseUp(object sender, MouseEventArgs e)
        {
            if (flag == true)
            {
                if (e.Button == MouseButtons.Left)
                {
                    p2 = e.Location;
                    drawing = false;
                }
            }
        }
        private void photo_box_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pic = sender as PictureBox;
            Pen pen = new Pen(Color.Green, 2);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;     //绘制线的格式
            if (drawing)
            {
                e.Graphics.DrawRectangle(pen, Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
            }
            pen.Dispose();
        }
        private void select_Click(object sender, EventArgs e)
        {
            flag = true;
            confirm.Visible = true;
            liuyifei = false;
        }
        private void Confirm_Click(object sender, EventArgs e)
        {
            Rect rect = new Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
            Bitmap bmp = new Bitmap(photo_box.Image);
            Mat src = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            Mat mode = new Mat(src, rect);
            Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mode);//MAT转bitmap
            bitmap1 = bitmap;

            int Height = bitmap1.Height;
            int Width = bitmap1.Width;
            Bitmap newBitmap = new Bitmap(Width, Height);
            Bitmap oldBitmap = (Bitmap)this.photo_box.Image;
            Color pixel;
            //拉普拉斯模板
            int[] Laplacian = { -1, -1, -1, -1, 9, -1, -1, -1, -1 };
            for (int x = 1; x < Width - 1; x++)
                for (int y = 1; y < Height - 1; y++)
                {
                    int r = 0, g = 0, b = 0;
                    int Index = 0;
                    for (int col = -1; col <= 1; col++)
                        for (int row = -1; row <= 1; row++)
                        {
                            pixel = oldBitmap.GetPixel(x + row, y + col);
                            r += pixel.R * Laplacian[Index];
                            g += pixel.G * Laplacian[Index];
                            b += pixel.B * Laplacian[Index];
                            Index++;
                        }
                    //处理颜色值溢出
                    r = r > 255 ? 255 : r;
                    r = r < 0 ? 0 : r;
                    g = g > 255 ? 255 : g;
                    g = g < 0 ? 0 : g;
                    b = b > 255 ? 255 : b;
                    b = b < 0 ? 0 : b;
                    newBitmap.SetPixel(x - 1, y - 1, Color.FromArgb(r, g, b));
                }



        model_box.Image = newBitmap;
            save_bmp.Visible = true;
            flag = false;
        }

        /*保存或加载模板*/
        private void Load_bmp_Click(object sender, EventArgs e)
        {
            string pathname = string.Empty;
            OpenFileDialog file = new OpenFileDialog();
            file.InitialDirectory = ".";
            file.Filter = "所有文件(*.*)|*.*";
            file.ShowDialog();
            if (file.FileName != string.Empty)
            {
                try
                {
                    pathname = file.FileName;
                    this.model_box.Load(pathname);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void Save_bmp_Click(object sender, EventArgs e)
        {
            if (model_box != null)
            {
                save_bmp.Visible = true;
                SaveFileDialog save = new SaveFileDialog();
                save.ShowDialog();
                if (save.FileName != string.Empty)
                {
                    model_box.Image.Save(save.FileName);
                }
            }
            else
            {
                save_bmp.Visible = false;
            }
        }

        /*模板匹配并显示结果*/
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            float a;
            a = trackBar1.Value / 100f;
            yuzhi.Text = "阈值大小：" + a.ToString();
            threshold = a;
        }
        private void Detection_Click(object sender, EventArgs e)
        {
            xinpianxuanze.Visible = true;
            double shijiey = 0, shijiex = 0;
            //匹配图
            Bitmap bmp1 = new Bitmap(model_box.Image);
            Mat temp = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp1);//bitmap转 mat

            //被匹配图
            Bitmap bmp3 = new Bitmap(photo_box.Image);
            bitmap2 = bmp3;
            int Height = bitmap2.Height;
            int Width = bitmap2.Width;
            Bitmap newBitmap1 = new Bitmap(Width, Height);
            Bitmap oldBitmap = (Bitmap)this.photo_box.Image;
            Color pixel;
            //拉普拉斯模板
            int[] Laplacian = { -1, -1, -1, -1, 9, -1, -1, -1, -1 };
            for (int x = 1; x < Width - 1; x++)
                for (int y = 1; y < Height - 1; y++)
                {
                    int r = 0, g = 0, b = 0;
                    int Index = 0;
                    for (int col = -1; col <= 1; col++)
                        for (int row = -1; row <= 1; row++)
                        {
                            pixel = oldBitmap.GetPixel(x + row, y + col);
                            r += pixel.R * Laplacian[Index];
                            g += pixel.G * Laplacian[Index];
                            b += pixel.B * Laplacian[Index];
                            Index++;
                        }
                    //处理颜色值溢出
                    r = r > 255 ? 255 : r;
                    r = r < 0 ? 0 : r;
                    g = g > 255 ? 255 : g;
                    g = g < 0 ? 0 : g;
                    b = b > 255 ? 255 : b;
                    b = b < 0 ? 0 : b;
                    newBitmap1.SetPixel(x - 1, y - 1, Color.FromArgb(r, g, b));
                }
            Bitmap bmp2 = new Bitmap(newBitmap1);
    
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
            if (shijie.Checked == true)//循环查找画框显示世界坐标系
            {
                for (int i = 1; i < result.Rows - temp.Rows; i += temp.Rows)//行遍历
                {
                    for (int j = 1; j < result.Cols - temp.Cols; j += temp.Cols)//列遍历
                    {
                        Rect roi = new Rect(j, i, temp.Cols, temp.Rows);        //建立感兴趣
                        Mat RoiResult = new Mat(result, roi);
                        Cv2.MinMaxLoc(RoiResult, out minVul, out maxVul, out minLoc, out maxLoc);//查找极值
                        if (maxVul > threshold)
                        {
                            //创建画板
                            Graphics g = photo_box.CreateGraphics();
                            //画出识别的矩形
                            g.DrawRectangle(new Pen(Color.Yellow), j + maxLoc.X, i + maxLoc.Y, Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                            //像素坐标转换成世界坐标
                            shijiex = Math.Round((250 - j - maxLoc.X) * kx + Convert.ToDouble(ans1));
                            shijiey = Math.Round((i + maxLoc.Y - 200) * ky + Convert.ToDouble(ans2));
                            string axis = '(' + Convert.ToString(shijiex + temp.Cols / 2) + ',' + Convert.ToString(shijiey + temp.Rows / 2) + ')';
                            //写出世界坐标
                            g.DrawString(axis, this.Font, Brushes.Yellow, j + maxLoc.X, i + maxLoc.Y - 10);
                            g.Dispose();
                        }
                    }
                }
            }
            if (xiangsu.Checked == true)//循环查找画框显示像素坐标系
            {
                for (int i = 1; i < result.Rows - temp.Rows; i += temp.Rows)//行遍历
                {
                    for (int j = 1; j < result.Cols - temp.Cols; j += temp.Cols)//列遍历
                    {
                        Rect roi = new Rect(j, i, temp.Cols, temp.Rows);        //建立感兴趣
                        Mat RoiResult = new Mat(result, roi);
                        Cv2.MinMaxLoc(RoiResult, out minVul, out maxVul, out minLoc, out maxLoc);//查找极值
                        if (maxVul > threshold)
                        {
                            //创建画板
                            Graphics g = photo_box.CreateGraphics();
                            //画出识别的矩形
                            g.DrawRectangle(new Pen(Color.Yellow), j + maxLoc.X, i + maxLoc.Y, Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                            string axis = '(' + Convert.ToString(j + maxLoc.X + temp.Cols / 2) + ',' + Convert.ToString(i + maxLoc.Y + temp.Rows / 2) + ')';
                            //写出像素坐标
                            g.DrawString(axis, this.Font, Brushes.Yellow, j + maxLoc.X, i + maxLoc.Y - 10);
                            g.Dispose();
                        }
                    }
                }
            }
        }

        /*pmac控制部分*/
        //HACK
        private void Select_device_Click(object sender, EventArgs e)
        {
            PMAC.SelectDevice(0, out pmacNumber, out selectPmacSuccess);
            select_device.Text = Convert.ToString(selectPmacSuccess);

            if (selectPmacSuccess)
            {
                PMAC.Open(pmacNumber, out openPmacSuccess);
                if (openPmacSuccess)
                {
                    this.DialogResult = DialogResult.OK;
                }
            }
            panel1.Visible = true;
            select_device.Visible = false;
            connect_cam.Visible = true;
            //this.DialogResult = DialogResult.OK;
        }

        private void XP_Click(object sender, EventArgs e)
        {
            string ans = null;
            if(comboBox1.Text!="")
            {
                if (comboBox1.Text == "100")
                {
                    PMAC.GetResponse(pmacNumber, "#1J^-100", out ans);
                }
                if (comboBox1.Text == "500")
                {
                    PMAC.GetResponse(pmacNumber, "#1J^-500", out ans);
                }
            }
            else
            {
                MessageBox.Show("请选择步距");
            }
        }
        private void XS_Click(object sender, EventArgs e)
        {
            string ans = null;
            if (comboBox1.Text != "")
            {
                if (comboBox1.Text == "100")
                {
                    PMAC.GetResponse(pmacNumber, "#1J^100", out ans);
                }
                if (comboBox1.Text == "500")
                {
                    PMAC.GetResponse(pmacNumber, "#1J^500", out ans);
                }
            }
            else
            {
                MessageBox.Show("请选择步距");
            }
        }
        private void YP_Click(object sender, EventArgs e)
        {
            string ans = null;
            if (comboBox1.Text != "")
            {
                if (comboBox1.Text == "100")
                {
                    PMAC.GetResponse(pmacNumber, "#2J^100", out ans);
                }
                if (comboBox1.Text == "500")
                {
                    PMAC.GetResponse(pmacNumber, "#2J^500", out ans);
                }
            }
            else
            {
                MessageBox.Show("请选择步距");
            }
        }
        private void YS_Click(object sender, EventArgs e)
        {
            string ans = null;
            if (comboBox1.Text != "")
            {
                if (comboBox1.Text == "100")
                {
                    PMAC.GetResponse(pmacNumber, "#2J^-100", out ans);
                }
                if (comboBox1.Text == "500")
                {
                    PMAC.GetResponse(pmacNumber, "#2J^-500", out ans);
                }
            }
            else
            {
                MessageBox.Show("请选择步距");
            }
        }
        private void XitaS_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#4J^-50", out ans);
        }
        private void XitaP_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#4J^50", out ans);
        }

        /*标定点选择和标定结果kx、ky*/
        private void Xuanzezuobiaodingdian_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#1P", out ans);
            zuo = Convert.ToDouble(ans);
        }
        private void Xuanzeyoubiaodingdian_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#1P", out ans);
            you = Convert.ToDouble(ans);
        }
        private void Xuanzeshangbiaodingdian_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#2P", out ans);
            shang = Convert.ToDouble(ans);
        }
        private void Xuanzexiabiaodingdian_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#2P", out ans);
            xia = Convert.ToDouble(ans);
        }
        private void Start_Click(object sender, EventArgs e)
        {
            label8.Visible = true;
            label9.Visible = true;
            label10.Visible = true;
            label11.Visible = true;
            label12.Visible = true;
            label7.Visible = true;
            end.Visible = true;
            panel2.Visible = true;
        }

        private void Xinpianxuanze_Click(object sender, EventArgs e)
        {
            pause.Visible = true;
            xiaoshimei = true;
            liuyifei = true;
        }


        private void End_Click(object sender, EventArgs e)
        {
            label8.Visible = false;
            label9.Visible = false;
            label10.Visible = false;
            label11.Visible = false;
            label12.Visible = false;
            label7.Visible = false;
            end.Visible = false;
            panel2.Visible = false;
        }
        private void Biaodingqueren_Click(object sender, EventArgs e)
        {
            kx = Math.Abs(you - zuo) / xxx;
            ky = Math.Abs(shang - xia) / yyy;
            panel3.Visible = true;
            dangliangx.Text = "kx:" + kx.ToString();
            dangliangy.Text = "ky:" + ky.ToString();
        }

        /*选择芯片并移动至中心*/
        //HACK
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (zhangzifeng == true)
            {
                string answer = null;
                camera.OneShot();
                oneshot.Visible = false;
                keepshot.Visible = false;
                PMAC.GetResponse(pmacNumber, "M514 = 1", out answer);
                PMAC.GetResponse(pmacNumber, "#1P", out ans1);
                PMAC.GetResponse(pmacNumber, "#2P", out ans2);

                Po1.Text = "#1:" + Math.Round(Convert.ToDouble(ans1)).ToString();
                Po2.Text = "#2:" + Math.Round(Convert.ToDouble(ans2)).ToString();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Add("100");
            comboBox1.Items.Add("500");
            connect_cam.Visible = false;
        }

        private void Home_Click(object sender, EventArgs e)
        {
            string fileDirectory, ans;
            bool downloadSuccess;
            string velosity1, velosity2, velosity3, velosity4;

            fileDirectory = @"C:\Users\汕头大学\Desktop\下位机\HMZ.txt";
            PMAC.Download(pmacNumber, fileDirectory, false, false, true, true, out downloadSuccess);
            PMAC.GetResponse(pmacNumber, "ENABLE PLC1,2,3,4", out ans);

            PMAC.GetResponse(pmacNumber, "#1v", out velosity1);
            PMAC.GetResponse(pmacNumber, "#2v", out velosity2);
            if (Convert.ToDouble(velosity1) == 0 && Convert.ToDouble(velosity2) == 0)
            {
                MessageBox.Show("回零完成！");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
           
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            if (xiaoshimei == true)
            {
                camera.OneShot();
                oneshot.Visible = false;
                keepshot.Visible = false;
            }
        }

        private void Timer3_Tick(object sender, EventArgs e)
        {
            if (xiaoshimei == true)
            {
                double shijiey = 0, shijiex = 0;
                //匹配图
                Bitmap bmp1 = new Bitmap(model_box.Image);
                Mat temp = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp1);//bitmap转 mat

                //被匹配图
                Bitmap bmp2 = new Bitmap(photo_box.Image);
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
                if (shijie.Checked == true)//循环查找画框显示世界坐标系
                {
                    for (int i = 1; i < result.Rows - temp.Rows; i += temp.Rows )//行遍历
                    {
                        for (int j = 1; j < result.Cols - temp.Cols; j += temp.Cols)//列遍历
                        {
                            Rect roi = new Rect(j, i, temp.Cols, temp.Rows);        //建立感兴趣
                            Mat RoiResult = new Mat(result, roi);
                            Cv2.MinMaxLoc(RoiResult, out minVul, out maxVul, out minLoc, out maxLoc);//查找极值
                            if (maxVul > threshold)
                            {
                                //创建画板
                                Graphics g = photo_box.CreateGraphics();
                                //画出识别的矩形
                                g.DrawRectangle(new Pen(Color.Yellow), j + maxLoc.X, i + maxLoc.Y, Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                                //像素坐标转换成世界坐标
                                shijiex = Math.Round((250 - j - maxLoc.X) * kx + Convert.ToDouble(ans1));
                                shijiey = Math.Round((i + maxLoc.Y - 200) * ky + Convert.ToDouble(ans2));
                                string axis = '(' + Convert.ToString(shijiex) + ',' + Convert.ToString(shijiey ) + ')';
                                //写出世界坐标
                                g.DrawString(axis, this.Font, Brushes.Yellow, j + maxLoc.X, i + maxLoc.Y - 10);
                                g.Dispose();
                            }
                        }
                    }
                }
                if (xiangsu.Checked == true)//循环查找画框显示像素坐标系
                {
                    for (int i = 1; i < result.Rows - temp.Rows; i += temp.Rows)//行遍历
                    {
                        for (int j = 1; j < result.Cols - temp.Cols; j += temp.Cols)//列遍历
                        {
                            Rect roi = new Rect(j, i, temp.Cols, temp.Rows);        //建立感兴趣
                            Mat RoiResult = new Mat(result, roi);
                            Cv2.MinMaxLoc(RoiResult, out minVul, out maxVul, out minLoc, out maxLoc);//查找极值
                            if (maxVul > threshold)
                            {
                                //创建画板
                                Graphics g = photo_box.CreateGraphics();
                                //画出识别的矩形
                                g.DrawRectangle(new Pen(Color.Yellow), j + maxLoc.X, i + maxLoc.Y, Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                                string axis = '(' + Convert.ToString(j + maxLoc.X) + ',' + Convert.ToString(i + maxLoc.Y) + ')';
                                //写出像素坐标
                                g.DrawString(axis, this.Font, Brushes.Yellow, j + maxLoc.X, i + maxLoc.Y - 10);
                                g.Dispose();
                            }
                        }
                    }
                }
            }
        }

        private void Photo_box_Click(object sender, EventArgs e)
        {
            string ans = null;
            System.Drawing.Point picMouse = photo_box.PointToClient(Control.MousePosition);
            tempx = picMouse.X;
            tempy = picMouse.Y;
            if (liuyifei == true)
            {
                MessageBox.Show("我要开始动了");
                double shijiex, shijiey;
                //tempx = Cursor.Position.X - 25;
                //tempy = Cursor.Position.Y - 25;
                PMAC.GetResponse(pmacNumber, "#1P", out ans1);
                PMAC.GetResponse(pmacNumber, "#2P", out ans2);

                shijiex = Math.Round((250 - tempx ) * kx);
                shijiey = Math.Round((tempy - 200 ) * ky);

                PMAC.GetResponse(pmacNumber, "I122 =5", out ans);
                PMAC.GetResponse(pmacNumber, "I222 =5", out ans);
                PMAC.GetResponse(pmacNumber, "#1J^" + shijiex.ToString(), out ans1);
                PMAC.GetResponse(pmacNumber, "#2J^" + shijiey.ToString(), out ans2);
            }
        }

    }

}
