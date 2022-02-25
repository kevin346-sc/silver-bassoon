using System;
using System.Drawing;
using System.Windows.Forms;
using PCOMMSERVERLib;
using OpenCvSharp;
using MySql.Data.MySqlClient;
using HalconDotNet;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Configuration;

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
        public double xxx = 300;//像素标定值
        public double yyy = 200;
        public double kx = 0;
        public double ky = 0;//标定结果
        public double kmx = 0.03;
        public double kmy = 0.05;//mapping图绘制的相对位置比例系数
        public double shijiex = 0;
        public double shijiey = 0;//存储xy坐标全局变量，mapping图计算用
        public long amount = 0; //全局变量存储芯片数量
        public double firstx = 0;
        public double firsty = 0;//记录mapping图的第一个点世界坐标
        public int cameras = 1;//防止重复keepshot
        public double  PathTopLeftx = 0;
        public double  PathTopLefty = 0;
        public double  PathTopRightx = 0;
        public double PathTopRighty = 0;
        public double  PathButtom = 0;
        public double  PathLeftToRight = 0;
        public double  PathTopToButtom = 0;
        public bool zhangzifeng = false;
        string ans1 = null;
        string ans2 = null;
        public double shimei1 = 0;
        public double shimei2 = 0;
        public bool mouse2 = false;
        public bool mouse3 = false;
        public Bitmap bitmap1;
        public Bitmap newbitmap1;
        public Bitmap bitmap2;
        public Bitmap newbitmap2;

        private System.Drawing.Point p1, p2;//定义两个点（启点，终点）  
        private static bool drawing = false;//设置一个启动标志  
        public bool flag = false;
        public int[] s = { 0, 0, 0 };
        public static int i = 1;
        public int lbname;//芯片像素坐标
        Mat temp;
        Double threshold = 0.8;//默认模板匹配阈值为0.8
        Class1 camera = new Class1();
        

        //初始加载界面
        public Form1()
        {
            InitializeComponent();
            PMAC = new PmacDeviceClass();
            camera.CameraImageEvent += Camera_CameraImageEvent;
            Unanble();
            yuzhi.Text = "阈值大小:0.8";
            pictureBox1.Width = 500;
            pictureBox1.Height = 400;
            photo_box.Width = 500;
            photo_box.Height = 400;
            panel4.Width = 500;
            panel4.Height = 300;
        }
        [DllImport("kernel32.dll")]
        static extern uint GetTickCount();

        static void delay(uint ms)
        {
            uint start = GetTickCount();
            while(GetTickCount() - start < ms)
            {
                Application.DoEvents();
            }
        }
        private void con_to_erjixm(string a, string x, string y)
        {
            string constr = ConfigurationManager.ConnectionStrings["connstr"].ConnectionString;
            MySqlConnection conn = null;
            MySqlCommand cmd = null;
            try
            {
                conn = new MySqlConnection(constr);
                conn.Open();
                string cmdString = "INSERT INTO chips VALUES (" + "'" + a + "'" + ',' + "'" + x + "'" + ',' + "'" + y + "'" + ");";
                cmd = new MySqlCommand(cmdString, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private void con_to_erjixm(string a)
        {
            string constr = ConfigurationManager.ConnectionStrings["connstr"].ConnectionString;
            MySqlConnection conn = null;
            MySqlCommand cmd = null;
            try
            {
                conn = new MySqlConnection(constr);
                conn.Open();
                string cmdString = "select * from chips where Chip_ID = '" + a + "';";
                cmd = new MySqlCommand(cmdString, conn);
                MySqlDataReader reader = cmd.ExecuteReader();//执行ExecuteReader()返回一个MySqlDataReader对象
                dataGridView2.Rows.Clear();
                while (reader.Read())
                {
                    int index = this.dataGridView2.Rows.Add();
                    this.dataGridView2.Rows[index].Cells[0].Value = reader.GetString("Chip_ID");
                    this.dataGridView2.Rows[index].Cells[1].Value = reader.GetString("XPosition");
                    this.dataGridView2.Rows[index].Cells[2].Value = reader.GetString("YPosition");
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
            finally
            {
                conn.Close();
            }
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
            pictureBox1.Invoke(new MethodInvoker(delegate
            {
                Bitmap old = pictureBox1.Image as Bitmap;
                pictureBox1.Image = bmp;
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
            panel1.Visible = false;
            panel2.Visible = false;
            panel3.Visible = false;
            Top_right.Visible = false;
            Buttom.Visible = false;
        }

        private void Shut_cam_Click(object sender, EventArgs e)
        {
            //string answer = null;
            camera.DestroyCamera();
            Unanble();
            connect_cam.Visible = true;
            photo_box.Image = null;
            //PMAC.GetResponse(pmacNumber, "M514 = 0", out answer);
            if (cameras < 1)
            {
                cameras++;
            }
        }
        private void Keepshot_Click_1(object sender, EventArgs e)
        {
            //string answer = null;
            if (cameras > 0)
            {
                camera.KeepShot();
                cameras--;
            }
            oneshot.Visible = false;
            keepshot.Visible = false;
            //PMAC.GetResponse(pmacNumber, "M514 = 1", out answer);
        }
        private void Oneshot_Click_1(object sender, EventArgs e)
        {
            if (cameras > 0)
            {
                //string answer = null;
                camera.OneShot();
                //PMAC.GetResponse(pmacNumber, "M514 = 1", out answer);
            }
            else
            {
                MessageBox.Show("先暂停连拍哇！");
            }
        }
        private void Pause_Click_1(object sender, EventArgs e)
        {
            camera.Stop();
            if (cameras == 0)
            {
                cameras++;
            }
            oneshot.Visible = true;
            keepshot.Visible = true;
            //PMAC.GetResponse(pmacNumber, "#1J/", out string answer);
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
        }
        private void Confirm_Click(object sender, EventArgs e)
        {
            Rect rect = new Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
            Bitmap bmp = new Bitmap(photo_box.Image);
            Mat src = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            Mat mode = new Mat(src, rect);
            Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mode);//MAT转bitmap
            Color pixel;
            string o = Convert.ToString(Math.Abs(p1.X - p2.X));
            string p = Convert.ToString(Math.Abs(p1.Y - p2.Y));
            label3.Text = (o);
            label4.Text = (p);
            int Height = bitmap.Height;
            int Width = bitmap.Width;
            Bitmap newBitmap = new Bitmap(Width, Height);
            Bitmap oldBitmap = (Bitmap)bitmap;
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


            bitmap1 = newBitmap;
            if (bitmap1 != null)
            {
                newbitmap1 = newBitmap.Clone() as Bitmap; //clone一个副本
                int width = newbitmap1.Width;
                int height = newbitmap1.Height;
                int size = width * height;
                //总像数个数
                int[] gray = new int[256];
                //定义一个int数组，用来存放各像元值的个数
                double[] graydense = new double[256];
                //定义一个float数组，存放每个灰度像素个数占比
                for (int i = 0; i < width; ++i)
                    for (int j = 0; j < height; ++j)
                    {
                        Color pixe1 = newbitmap1.GetPixel(i, j);
                        //计算各像元值的个数
                        gray[Convert.ToInt16(pixe1.R)] += 1;
                        //由于是灰度只读取R值
                    }
                for (int i = 0; i < 256; i++)
                {
                    graydense[i] = (gray[i] * 1.0) / size;
                    //每个灰度像素个数占比
                }

                for (int i = 1; i < 256; i++)
                {
                    graydense[i] = graydense[i] + graydense[i - 1];
                    //累计百分比
                }

                for (int i = 0; i < width; ++i)
                    for (int j = 0; j < height; ++j)
                    {
                        Color pixe1 = newbitmap1.GetPixel(i, j);
                        int oldpixel = Convert.ToInt16(pixe1.R); //原始灰度
                        int newpixel = 0;
                        if (oldpixel == 0)
                            newpixel = 0;
                        //如果原始灰度值为0则变换后也为0
                        else
                            newpixel = Convert.ToInt16(graydense[Convert.ToInt16(pixe1.R)] * 255);
                        //如果原始灰度不为0，则执行变换公式为   <新像元灰度 = 原始灰度 * 累计百分比>
                        pixel = Color.FromArgb(newpixel, newpixel, newpixel);
                        newbitmap1.SetPixel(i, j, pixel); //读入newbitmap
                    }
            }

            int Height2 = newbitmap1.Height;
            int Width2 = newbitmap1.Width;
            Bitmap newbitmap3 = new Bitmap(Width2, Height2);
            Bitmap oldbitmap1 = (Bitmap)newbitmap1;
            Color pixe2;
            for (int x = 1; x < Width2; x++)
            {
                for (int y = 1; y < Height2; y++)
                {
                    int r, g, b;
                    pixe2 = oldbitmap1.GetPixel(x, y);
                    r = 255 - pixe2.R;
                    g = 255 - pixe2.G;
                    b = 255 - pixe2.B;
                    newbitmap3.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            model_box.Image = newbitmap3;
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
            if (model_box.Image != null)
            {
                SaveFileDialog save = new SaveFileDialog();
                save.ShowDialog();
                if (save.FileName != string.Empty)
                {
                    model_box.Image.Save(save.FileName);
                    MessageBox.Show("保存成功！");
                }
            }
            else
            {
                MessageBox.Show("图都没有，保存条毛啊！");
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
            if (this.model_box.Image != null)
            {
                //匹配图
                Bitmap bmp1 = new Bitmap(model_box.Image);
                temp = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp1);//bitmap转 mat

                //被匹配图
            
                Bitmap bmp4 = new Bitmap(photo_box.Image);
                int Height = bmp4.Height;
                int Width = bmp4.Width;
                Bitmap newBitmap3 = new Bitmap(Width, Height);
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
                        newBitmap3.SetPixel(x - 1, y - 1, Color.FromArgb(r, g, b));
                    }
                bitmap2 = newBitmap3;
                if (bitmap2 != null)
                {
                    newbitmap2 = newBitmap3.Clone() as Bitmap; //clone一个副本
                    int width = newbitmap2.Width;
                    int height = newbitmap2.Height;
                    int size = width * height;
                    //总像数个数
                    int[] gray = new int[256];
                    //定义一个int数组，用来存放各像元值的个数
                    double[] graydense = new double[256];
                    //定义一个float数组，存放每个灰度像素个数占比
                    for (int i = 0; i < width; ++i)
                        for (int j = 0; j < height; ++j)
                        {
                            Color pixe1 = newbitmap2.GetPixel(i, j);
                            //计算各像元值的个数
                            gray[Convert.ToInt16(pixe1.R)] += 1;
                            //由于是灰度只读取R值
                        }
                    for (int i = 0; i < 256; i++)
                    {
                        graydense[i] = (gray[i] * 1.0) / size;
                        //每个灰度像素个数占比
                    }

                    for (int i = 1; i < 256; i++)
                    {
                        graydense[i] = graydense[i] + graydense[i - 1];
                        //累计百分比
                    }

                    for (int i = 0; i < width; ++i)
                        for (int j = 0; j < height; ++j)
                        {
                            Color pixe1 = newbitmap2.GetPixel(i, j);
                            int oldpixel = Convert.ToInt16(pixe1.R); //原始灰度
                            int newpixel = 0;
                            if (oldpixel == 0)
                                newpixel = 0;
                            //如果原始灰度值为0则变换后也为0
                            else
                                newpixel = Convert.ToInt16(graydense[Convert.ToInt16(pixe1.R)] * 255);
                            //如果原始灰度不为0，则执行变换公式为   <新像元灰度 = 原始灰度 * 累计百分比>
                            pixel = Color.FromArgb(newpixel, newpixel, newpixel);
                            newbitmap2.SetPixel(i, j, pixel); //读入newbitmap
                        }
                }

                int Height1 = newbitmap2.Height;
                int Width1 = newbitmap2.Width;
                Bitmap newbitmap5 = new Bitmap(Width, Height);
                Bitmap oldbitmap = (Bitmap)newbitmap2;
                Color pixe2;
                for (int x = 1; x < Width1; x++)
                {
                    for (int y = 1; y < Height1; y++)
                    {
                        int r, g, b;
                        pixe2 = oldbitmap.GetPixel(x, y);
                        r = 255 - pixe2.R;
                        g = 255 - pixe2.G;
                        b = 255 - pixe2.B;
                        newbitmap5.SetPixel(x, y, Color.FromArgb(r, g, b));
                    }
                }


                Bitmap bmp2 = new Bitmap(newbitmap5);
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
                    PMAC.GetResponse(pmacNumber, "#1P", out ans1);
                    PMAC.GetResponse(pmacNumber, "#2P", out ans2);
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
                                shijiex = Math.Round((j + maxLoc.X) * kx + Convert.ToDouble(ans1));
                                shijiey = Math.Round((i + maxLoc.Y) * (-1) * ky + Convert.ToDouble(ans2));
                                string axis = '(' + Convert.ToString(shijiex) + ',' + Convert.ToString(shijiey) + ')';
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
            else
            {
                MessageBox.Show("模板都没有，识别条毛啊！");
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
            this.DialogResult = DialogResult.OK;
        }
        private void XP_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#1J^-500", out ans);
        }
        private void XS_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#1J^+500", out ans);
        }
        private void YP_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#2J^500", out ans);
        }
        private void YS_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#2J^-500", out ans);
        }
        private void XitaS_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#4J^-500", out ans);
        }
        private void XitaP_Click(object sender, EventArgs e)
        {
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#4J^500", out ans);
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
            photo_box.Location = new Point(40, 30);
            label10.Location = new Point(290, 30);
            label8.Location = new Point(40, 230);
            label7.Location = new Point(140, 130);
            label9.Location = new Point(140, 330);
            label11.Location = new Point(140, 130);
            label12.Location = new Point(440, 130);
            label10.Size = new Size(2, 400);
            label8.Size = new Size(500, 2);
            label7.Size = new Size(300,2);
            label9.Size = new Size(300,2);
            label11.Size = new Size(2, 200);
            label12.Size =new Size(2,200);
            
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

        private void create_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.ShowDialog();
        }


        /*private void Mapping_Paint(double x, double y, int a)
        {
            if (a == 1)
            {
                firstx = x;
                firsty = y;
            }

                int w = 6; // 矩形大小
            Bitmap bm = new Bitmap(this.pictureBox2.ClientRectangle.Width, this.pictureBox2.ClientRectangle.Height);
            Graphics mapping = Graphics.FromImage(bm);
            mapping.Clear(Color.Black);
            Pen p = new Pen(Color.Blue, 1);//设置描边画笔
            SolidBrush b = new SolidBrush(Color.Green);//设置填充画笔
            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();

            //每个小矩形大小为6，每个小矩形占用8*8面积，mapping图总面积=半圆面积为π*300*300/2 = 141,371.669，最多可存储2,208个小矩形（芯片）

            double Px = (x - firstx) * 0.11;
            double Py = (y - firsty) * 0.11;//世界坐标转化成mapping的坐标

            gp.AddRectangle(new Rectangle(Convert.ToInt32(Px), Convert.ToInt32(Py), w, w));

            mapping.DrawPath(p, gp);

            mapping.FillPath(b, gp);

            this.pictureBox2.Image = bm;
        }*/

        private void home_Click(object sender, EventArgs e)// 回零按钮
        {
            string fileDirectory, ans;
            bool downloadSuccess;

            fileDirectory = @"C:\Users\amour\Desktop\HMZ.txt";
            PMAC.Download(pmacNumber, fileDirectory, false, false, true, true, out downloadSuccess);
            PMAC.GetResponse(pmacNumber, "ENABLE PLC1,2,3,4", out ans);
        }
        
        private void scanning_Click(object sender, EventArgs e)
        {
            camera.Stop();
            /*扫描路径*/
            string ans = null;
            double m = (PathLeftToRight) / (Convert.ToInt32(Math.Abs(500 - Math.Abs(p1.X - p2.X))) * kx)+1;//计算扫描所需列数（即横向视窗多少）
            double n = (PathTopToButtom) / (Convert.ToInt32(400 - Math.Abs(p1.Y - p2.Y)) * ky)+1;//计算扫描所需行数
            double distancex = 0;
            double distancey = 0;
            //开始图像识别
            for (int l = 1; l < n; l++ )
            {
                PMAC.GetResponse(pmacNumber, "I122 = 20", out ans);
                PMAC.GetResponse(pmacNumber, "I222 = 20", out ans);
                //distance = PathTopToButtom * p;
                distancex = Math.Abs(500 - Math.Abs(p1.X - p2.X)) * kx;
                distancey = Math.Abs((400 - Math.Abs(p1.Y - p2.Y)) * ky);
                double zuodaoyou=PathLeftToRight+ Math.Abs(500 - Math.Abs(p1.X - p2.X)) *kx;
                kmx = panel4.Width / zuodaoyou;
                kmy = panel4.Height / PathTopToButtom;
                if (l % 2 != 0)//奇数次行
                {
                    for(int k=1;k<m;k++)
                    {
                        camera.OneShot();
                        delay(2000);
                        if (model_box.Image != null)//防止没有模板而报错
                        {
                            
                            //图像识别并存入数据库
                            Bitmap bmp1 = new Bitmap(model_box.Image);
                            temp = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp1);//bitmap转 mat

                            //被匹配图
                            //图像预处理
                            Bitmap bmp4 = new Bitmap(pictureBox1.Image);
                            int Height = bmp4.Height;
                            int Width = bmp4.Width;
                            Bitmap newBitmap3 = new Bitmap(Width, Height);
                            Bitmap oldBitmap = (Bitmap)this.pictureBox1.Image;
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
                                    newBitmap3.SetPixel(x - 1, y - 1, Color.FromArgb(r, g, b));
                                }
                            bitmap2 = newBitmap3;
                            if (bitmap2 != null)
                            {
                                newbitmap2 = newBitmap3.Clone() as Bitmap; //clone一个副本
                                int width = newbitmap2.Width;
                                int height = newbitmap2.Height;
                                int size = width * height;
                                //总像数个数
                                int[] gray = new int[256];
                                //定义一个int数组，用来存放各像元值的个数
                                double[] graydense = new double[256];
                                //定义一个float数组，存放每个灰度像素个数占比
                                for (int i = 0; i < width; ++i)
                                    for (int j = 0; j < height; ++j)
                                    {
                                        Color pixe1 = newbitmap2.GetPixel(i, j);
                                        //计算各像元值的个数
                                        gray[Convert.ToInt16(pixe1.R)] += 1;
                                        //由于是灰度只读取R值
                                    }
                                for (int i = 0; i < 256; i++)
                                {
                                    graydense[i] = (gray[i] * 1.0) / size;
                                    //每个灰度像素个数占比
                                }

                                for (int i = 1; i < 256; i++)
                                {
                                    graydense[i] = graydense[i] + graydense[i - 1];
                                    //累计百分比
                                }

                                for (int i = 0; i < width; ++i)
                                    for (int j = 0; j < height; ++j)
                                    {
                                        Color pixe1 = newbitmap2.GetPixel(i, j);
                                        int oldpixel = Convert.ToInt16(pixe1.R); //原始灰度
                                        int newpixel = 0;
                                        if (oldpixel == 0)
                                            newpixel = 0;
                                        //如果原始灰度值为0则变换后也为0
                                        else
                                            newpixel = Convert.ToInt16(graydense[Convert.ToInt16(pixe1.R)] * 255);
                                        //如果原始灰度不为0，则执行变换公式为   <新像元灰度 = 原始灰度 * 累计百分比>
                                        pixel = Color.FromArgb(newpixel, newpixel, newpixel);
                                        newbitmap2.SetPixel(i, j, pixel); //读入newbitmap
                                    }
                            }

                            int Height1 = newbitmap2.Height;
                            int Width1 = newbitmap2.Width;
                            Bitmap newbitmap5 = new Bitmap(Width, Height);
                            Bitmap oldbitmap = (Bitmap)newbitmap2;
                            Color pixe2;
                            for (int x = 1; x < Width1; x++)
                            {
                                for (int y = 1; y < Height1; y++)
                                {
                                    int r, g, b;
                                    pixe2 = oldbitmap.GetPixel(x, y);
                                    r = 255 - pixe2.R;
                                    g = 255 - pixe2.G;
                                    b = 255 - pixe2.B;
                                    newbitmap5.SetPixel(x, y, Color.FromArgb(r, g, b));
                                }
                            }
                            Bitmap bmp2 = new Bitmap(newbitmap5);
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
                            PMAC.GetResponse(pmacNumber, "#1P", out ans1);
                            PMAC.GetResponse(pmacNumber, "#2P", out ans2);
                            for (int i = 1; i < result.Rows - temp.Rows; i += temp.Rows)//行遍历
                            {
                                for (int j = 1; j < result.Cols - temp.Cols; j += temp.Cols)//列遍历
                                {
                                    Rect roi = new Rect(j, i, temp.Cols, temp.Rows);        //建立感兴趣
                                    Mat RoiResult = new Mat(result, roi);
                                    Cv2.MinMaxLoc(RoiResult, out minVul, out maxVul, out minLoc, out maxLoc);//查找极值
                                    if (maxVul > threshold)
                                    {
                                        Graphics g = pictureBox1.CreateGraphics();
                                        //画出识别的矩形
                                        g.DrawRectangle(new Pen(Color.Yellow), j + maxLoc.X, i + maxLoc.Y, Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                                        //像素坐标转换成世界坐标
                                        shijiex = Math.Round((j + maxLoc.X) * kx + Convert.ToDouble(ans1));
                                        shijiey = Math.Round((i + maxLoc.Y) *(-1)* ky + Convert.ToDouble(ans2));
                                        string axis = '(' + Convert.ToString(shijiex) + ',' + Convert.ToString(shijiey) + ')';
                                        //写出世界坐标
                                        g.DrawString(axis, this.Font, Brushes.Yellow, j + maxLoc.X, i + maxLoc.Y - 10);
                                        g.Dispose();

                                        //识别图像同时把芯片数据存入数据库

                                        int rate = i / 1263;//记录扫描进度
                                        string cmdString = "";
                                        int w = 3;
                                        Size size = new Size(w, w);
                                        Label lable = new Label();
                                        lable.Location = new Point(Convert.ToInt32(Math.Abs(((shijiex - firstx) * kmx))), Convert.ToInt32(Math.Abs(((shijiey - firsty) * kmx))));
                                        lable.Size = size;
                                        // lable.Tag = lable.Size.Width.ToString() + ',' + lable.Size.Height.ToString();
                                        lable.BackColor = Color.Green;
                                        lable.Visible = true;
                                        panel4.Controls.Add(lable);
                                        // label4.Text = Convert.ToInt32((shijiey - firsty) * kmy).ToString();
                                        // label5.Text = Convert.ToInt32((shijiex - firstx) * kmx).ToString();
                                        con_to_erjixm(amount.ToString(), shijiex.ToString(), shijiey.ToString());
                                        amount += 1;
                                        
                                    }
                                }
                            }
                            delay(1000);
                        }
                        PMAC.GetResponse(pmacNumber, "#1J^" + distancex.ToString(), out ans);
                        delay(2000);
                    }
                    PMAC.GetResponse(pmacNumber, "#2J^-"+ distancey.ToString(), out ans);
                    delay(2000);
                    //PMAC.GetResponse(pmacNumber, "#1J^-10000", out ans);//扫描完成一行后每次向下、向中心移动
                }
                else if (l % 2 == 0)//偶数行反向运动
                {
                    for (int k = 1; k < m; k++)
                    {
                        if (model_box.Image != null)//防止没有模板而报错
                        {
                            camera.OneShot();
                            delay(2000);
                            //图像识别并存入数据库
                            Bitmap bmp1 = new Bitmap(model_box.Image);
                            temp = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp1);//bitmap转 mat

                            //被匹配图

                            Bitmap bmp4 = new Bitmap(pictureBox1.Image);
                            int Height = bmp4.Height;
                            int Width = bmp4.Width;
                            Bitmap newBitmap3 = new Bitmap(Width, Height);
                            Bitmap oldBitmap = (Bitmap)this.pictureBox1.Image;
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
                                    newBitmap3.SetPixel(x - 1, y - 1, Color.FromArgb(r, g, b));
                                }
                            bitmap2 = newBitmap3;
                            if (bitmap2 != null)
                            {
                                newbitmap2 = newBitmap3.Clone() as Bitmap; //clone一个副本
                                int width = newbitmap2.Width;
                                int height = newbitmap2.Height;
                                int size = width * height;
                                //总像数个数
                                int[] gray = new int[256];
                                //定义一个int数组，用来存放各像元值的个数
                                double[] graydense = new double[256];
                                //定义一个float数组，存放每个灰度像素个数占比
                                for (int i = 0; i < width; ++i)
                                    for (int j = 0; j < height; ++j)
                                    {
                                        Color pixe1 = newbitmap2.GetPixel(i, j);
                                        //计算各像元值的个数
                                        gray[Convert.ToInt16(pixe1.R)] += 1;
                                        //由于是灰度只读取R值
                                    }
                                for (int i = 0; i < 256; i++)
                                {
                                    graydense[i] = (gray[i] * 1.0) / size;
                                    //每个灰度像素个数占比
                                }

                                for (int i = 1; i < 256; i++)
                                {
                                    graydense[i] = graydense[i] + graydense[i - 1];
                                    //累计百分比
                                }

                                for (int i = 0; i < width; ++i)
                                    for (int j = 0; j < height; ++j)
                                    {
                                        Color pixe1 = newbitmap2.GetPixel(i, j);
                                        int oldpixel = Convert.ToInt16(pixe1.R); //原始灰度
                                        int newpixel = 0;
                                        if (oldpixel == 0)
                                            newpixel = 0;
                                        //如果原始灰度值为0则变换后也为0
                                        else
                                            newpixel = Convert.ToInt16(graydense[Convert.ToInt16(pixe1.R)] * 255);
                                        //如果原始灰度不为0，则执行变换公式为   <新像元灰度 = 原始灰度 * 累计百分比>
                                        pixel = Color.FromArgb(newpixel, newpixel, newpixel);
                                        newbitmap2.SetPixel(i, j, pixel); //读入newbitmap
                                    }
                            }

                            int Height1 = newbitmap2.Height;
                            int Width1 = newbitmap2.Width;
                            Bitmap newbitmap5 = new Bitmap(Width, Height);
                            Bitmap oldbitmap = (Bitmap)newbitmap2;
                            Color pixe2;
                            for (int x = 1; x < Width1; x++)
                            {
                                for (int y = 1; y < Height1; y++)
                                {
                                    int r, g, b;
                                    pixe2 = oldbitmap.GetPixel(x, y);
                                    r = 255 - pixe2.R;
                                    g = 255 - pixe2.G;
                                    b = 255 - pixe2.B;
                                    newbitmap5.SetPixel(x, y, Color.FromArgb(r, g, b));
                                }
                            }


                            Bitmap bmp2 = new Bitmap(newbitmap5);
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
                            PMAC.GetResponse(pmacNumber, "#1P", out ans1);
                            PMAC.GetResponse(pmacNumber, "#2P", out ans2);
                            for (int i = 1; i < result.Rows - temp.Rows; i += temp.Rows)//行遍历
                            {
                                for (int j = 1; j < result.Cols - temp.Cols; j += temp.Cols)//列遍历
                                {
                                    Rect roi = new Rect(j, i, temp.Cols, temp.Rows);        //建立感兴趣
                                    Mat RoiResult = new Mat(result, roi);
                                    Cv2.MinMaxLoc(RoiResult, out minVul, out maxVul, out minLoc, out maxLoc);//查找极值
                                    if (maxVul > threshold)
                                    {
                                        Graphics g = pictureBox1.CreateGraphics();
                                        //画出识别的矩形
                                        g.DrawRectangle(new Pen(Color.Yellow), j + maxLoc.X, i + maxLoc.Y, Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                                        //像素坐标转换成世界坐标
                                        shijiex = Math.Round((j + maxLoc.X) * kx + Convert.ToDouble(ans1));
                                        shijiey = Math.Round((i + maxLoc.Y) * (-1) * ky + Convert.ToDouble(ans2));
                                        string axis = '(' + Convert.ToString(shijiex) + ',' + Convert.ToString(shijiey) + ')';
                                        //写出世界坐标
                                        g.DrawString(axis, this.Font, Brushes.Yellow, j + maxLoc.X, i + maxLoc.Y - 10);
                                        g.Dispose();
                                        //识别图像同时把芯片数据存入数据库
                                        int rate = i / 1263;//记录扫描进度
                                        //string cmdString = "";
                                        int w = 3;
                                        Size size = new Size(w, w);
                                        Label lable = new Label();
                                        lable.Location = new Point(Convert.ToInt32(Math.Abs(((shijiex - firstx) * kmx))), Convert.ToInt32(Math.Abs(((shijiey - firsty) * kmx))));
                                        lable.Size = size;
                                        // lable.Tag = lable.Size.Width.ToString() + ',' + lable.Size.Height.ToString();
                                        lable.BackColor = Color.Green;
                                        lable.Visible = true;
                                        panel4.Controls.Add(lable);
                                        // label4.Text = Convert.ToInt32((shijiey - firsty) * kmy).ToString();
                                        // label5.Text = Convert.ToInt32((shijiex - firstx) * kmx).ToString();
                                        con_to_erjixm(amount.ToString(), shijiex.ToString(), shijiey.ToString());

                                        amount += 1;
                                        
                                    }
                                }

                            }
                        }
                        PMAC.GetResponse(pmacNumber, "#1J^-" + distancex.ToString(), out ans);
                        delay(1000);
                    }
                    PMAC.GetResponse(pmacNumber, "#2J^-" + distancey.ToString(), out ans);
                    delay(2000);
                    //PMAC.GetResponse(pmacNumber, "#1J^-10000", out ans);//扫描完成一行后每次向下、向中心移动
                }
            }
        }

        private void Biaodingqueren_Click(object sender, EventArgs e)
        {
            kx = 27.04;
            ky = 26.97;
            //kx = Math.Abs(you - zuo) / xxx;
            //ky = Math.Abs(shang - xia) / yyy;
            panel3.Visible = true;
            dangliangx.Text = "kx:" + kx.ToString();
            dangliangy.Text = "ky:" + ky.ToString();
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

        private void timer1_Tick(object sender, EventArgs e)
        {
            

            /*检测mapping有无点击*/

        }

        private void Top_left_Click(object sender, EventArgs e)
        {
            //string ans = null;

             
            //PMAC.GetResponse(pmacNumber, "#1P", out ans);
            //PathTopLeftx = Convert.ToDouble(ans);
            PathTopLeftx = 413660.8;
            PathTopLefty = 398056.4;
            //PMAC.GetResponse(pmacNumber, "#2P", out ans);
            //PathTopLefty = Convert.ToDouble(ans);
            Top_left.BackColor = Color.Green;
            Top_left.Visible = false;
            Top_right.Visible = true;
        }

        private void Top_right_Click(object sender, EventArgs e)
        {
           // string ans = null;
            //PMAC.GetResponse(pmacNumber, "#1P", out ans);
            //PathTopRightx = Convert.ToDouble(ans);
            //PMAC.GetResponse(pmacNumber, "#2P", out ans);
            //PathTopRighty = Convert.ToDouble(ans);
            PathTopRightx = 628628.8;
            PathTopRighty = 398056.4;
            firstx = 413660.8;
            firsty = 398056.4;
            PathLeftToRight = Math.Abs(PathTopRightx - PathTopLeftx);
            Top_right.BackColor = Color.Green;
            Top_right.Visible = false;
            Buttom.Visible = true;
        }

        private void Buttom_Click(object sender, EventArgs e)
        {
            string ans = null;
            //PMAC.GetResponse(pmacNumber, "#2P", out ans);
            //PathButtom = Convert.ToDouble(ans);
            PathButtom = 340051.3;
            PathTopToButtom = Math.Abs(PathTopRighty - PathButtom);
            Buttom.BackColor = Color.Green;
            Buttom.Visible = false;
            Top_left.Visible = true;
            PMAC.GetResponse(pmacNumber, "I122 = 15", out ans);
            PMAC.GetResponse(pmacNumber, "I222 = 15", out ans);
            PMAC.GetResponse(pmacNumber, "#1J=" + PathTopLeftx.ToString(), out ans);
            PMAC.GetResponse(pmacNumber, "#2J=" + PathTopLefty.ToString(), out ans);//回到扫描起点
            double zuodaoyou = PathLeftToRight + Math.Abs(500 - Math.Abs(p1.X - p2.X)) * kx;
            kmx = panel4.Width / zuodaoyou;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            zhangzifeng = false;
        }

        private void panel4_Click(object sender, EventArgs e)
        {
            if(mouse3)
            {
                string ans = null;
                double shubiaox;
                double shubiaoy;
                System.Drawing.Point picMouse = panel4.PointToClient(Control.MousePosition);
                shubiaox = picMouse.X;
                shubiaoy = picMouse.Y;
                label14.Text = (shubiaox.ToString() + ","+shubiaoy.ToString());
                shimei1 = shubiaox / kmx + firstx;
                shimei2 = -1*shubiaoy / kmx + firsty;
                label6.Text = ("芯片位置：(" + shimei1.ToString() + "," + shimei2.ToString() + ")");
                double motorx = shimei1 - 250 * kx;
                double motory = shimei2 + 200 * ky;
                PMAC.GetResponse(pmacNumber, "#1J=" + motorx.ToString(), out ans);
                PMAC.GetResponse(pmacNumber, "#2J=" + motory.ToString(), out ans);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            double rightgo = 0;
            rightgo = Math.Abs((500 - Math.Abs(p1.X - p2.X)) * kx); 
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#1J^" + rightgo.ToString(), out ans);
            delay(1000);
            camera.OneShot();
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            double downgo = 0;
            downgo = Math.Abs((400 - Math.Abs(p1.Y - p2.Y)) * ky);
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#2J^-" + downgo.ToString(), out ans);
            delay(1000);
            camera.OneShot();

        }

        private void button3_Click(object sender, EventArgs e)
        {
            double leftgo = 0;
            leftgo = Math.Abs((500- Math.Abs(p1.X - p2.X)) * kx);
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#1J^-" + leftgo.ToString(), out ans);
            delay(1000);
            camera.OneShot();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            double upgo = 0;
            upgo = Math.Abs((400- Math.Abs(p1.Y - p2.Y)) * ky);
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#2J^" + upgo.ToString(), out ans);
            delay(1000);
            camera.OneShot();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            int oo = (Math.Abs(p1.X - p2.X))*7+1;
            int pp = (Math.Abs(p1.Y - p2.Y))*6;
            camera.changWidth(oo);
            camera.kuanHeight(pp);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (cameras > 0)
            {
                //string answer = null;
                camera.OneShot();
                //PMAC.GetResponse(pmacNumber, "M514 = 1", out answer);
            }
            else
            {
                MessageBox.Show("先暂停连拍哇！");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            double rightgo = 0;
            rightgo = Math.Abs((500 - Math.Abs(p1.X - p2.X)) * kx);
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#1J^" + rightgo.ToString(), out ans);
            delay(1000);
            camera.OneShot();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            double downgo = 0;
            downgo = Math.Abs((400 - Math.Abs(p1.Y - p2.Y)) * ky);
            string ans = null;
            PMAC.GetResponse(pmacNumber, "#2J^-" + downgo.ToString(), out ans);
            delay(1000);
            camera.OneShot();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            //图像识别并存入数据库
            Bitmap bmp1 = new Bitmap(model_box.Image);
            temp = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp1);//bitmap转 mat

            //被匹配图
            //图像预处理
            Bitmap bmp4 = new Bitmap(pictureBox1.Image);
            int Height = bmp4.Height;
            int Width = bmp4.Width;
            Bitmap newBitmap3 = new Bitmap(Width, Height);
            Bitmap oldBitmap = (Bitmap)this.pictureBox1.Image;
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
                    newBitmap3.SetPixel(x - 1, y - 1, Color.FromArgb(r, g, b));
                }
            bitmap2 = newBitmap3;
            if (bitmap2 != null)
            {
                newbitmap2 = newBitmap3.Clone() as Bitmap; //clone一个副本
                int width = newbitmap2.Width;
                int height = newbitmap2.Height;
                int size = width * height;
                //总像数个数
                int[] gray = new int[256];
                //定义一个int数组，用来存放各像元值的个数
                double[] graydense = new double[256];
                //定义一个float数组，存放每个灰度像素个数占比
                for (int i = 0; i < width; ++i)
                    for (int j = 0; j < height; ++j)
                    {
                        Color pixe1 = newbitmap2.GetPixel(i, j);
                        //计算各像元值的个数
                        gray[Convert.ToInt16(pixe1.R)] += 1;
                        //由于是灰度只读取R值
                    }
                for (int i = 0; i < 256; i++)
                {
                    graydense[i] = (gray[i] * 1.0) / size;
                    //每个灰度像素个数占比
                }

                for (int i = 1; i < 256; i++)
                {
                    graydense[i] = graydense[i] + graydense[i - 1];
                    //累计百分比
                }

                for (int i = 0; i < width; ++i)
                    for (int j = 0; j < height; ++j)
                    {
                        Color pixe1 = newbitmap2.GetPixel(i, j);
                        int oldpixel = Convert.ToInt16(pixe1.R); //原始灰度
                        int newpixel = 0;
                        if (oldpixel == 0)
                            newpixel = 0;
                        //如果原始灰度值为0则变换后也为0
                        else
                            newpixel = Convert.ToInt16(graydense[Convert.ToInt16(pixe1.R)] * 255);
                        //如果原始灰度不为0，则执行变换公式为   <新像元灰度 = 原始灰度 * 累计百分比>
                        pixel = Color.FromArgb(newpixel, newpixel, newpixel);
                        newbitmap2.SetPixel(i, j, pixel); //读入newbitmap
                    }
            }

            int Height1 = newbitmap2.Height;
            int Width1 = newbitmap2.Width;
            Bitmap newbitmap5 = new Bitmap(Width, Height);
            Bitmap oldbitmap = (Bitmap)newbitmap2;
            Color pixe2;
            for (int x = 1; x < Width1; x++)
            {
                for (int y = 1; y < Height1; y++)
                {
                    int r, g, b;
                    pixe2 = oldbitmap.GetPixel(x, y);
                    r = 255 - pixe2.R;
                    g = 255 - pixe2.G;
                    b = 255 - pixe2.B;
                    newbitmap5.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            Bitmap bmp2 = new Bitmap(newbitmap5);
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
            PMAC.GetResponse(pmacNumber, "#1P", out ans1);
            PMAC.GetResponse(pmacNumber, "#2P", out ans2);

            for (int i = 1; i < result.Rows - temp.Rows; i += temp.Rows)//行遍历
            {
                for (int j = 1; j < result.Cols - temp.Cols; j += temp.Cols)//列遍历
                {
                    Rect roi = new Rect(j, i, temp.Cols, temp.Rows);        //建立感兴趣
                    Mat RoiResult = new Mat(result, roi);
                    Cv2.MinMaxLoc(RoiResult, out minVul, out maxVul, out minLoc, out maxLoc);//查找极值
                    if (maxVul > threshold)
                    {
                        Graphics g = pictureBox1.CreateGraphics();
                        //画出识别的矩形
                        g.DrawRectangle(new Pen(Color.Yellow), j + maxLoc.X, i + maxLoc.Y, Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                        //像素坐标转换成世界坐标
                        shijiex = Math.Round((j + maxLoc.X) * kx + Convert.ToDouble(ans1));
                        shijiey = Math.Round((i + maxLoc.Y) * (-1) * ky + Convert.ToDouble(ans2));
                        string axis = '(' + Convert.ToString(shijiex) + ',' + Convert.ToString(shijiey) + ')';
                        //写出世界坐标
                        g.DrawString(axis, this.Font, Brushes.Yellow, j + maxLoc.X, i + maxLoc.Y - 10);
                        g.Dispose();

                        //识别图像同时把芯片数据存入数据库

                        int rate = i / 1263;//记录扫描进度

                        int w = 3;
                        Size size = new Size(w, w);
                        Label lable = new Label();
                        lable.Location = new Point(Convert.ToInt32(Math.Abs(((shijiex - firstx) * kmx))), Convert.ToInt32(Math.Abs(((shijiey - firsty) * kmx))));
                        lable.Size = size;
                        // lable.Tag = lable.Size.Width.ToString() + ',' + lable.Size.Height.ToString();
                        lable.BackColor = Color.Green;
                        lable.Visible = true;
                        panel4.Controls.Add(lable);
                        // label4.Text = Convert.ToInt32((shijiey - firsty) * kmy).ToString();
                        // label5.Text = Convert.ToInt32((shijiex - firstx) * kmx).ToString();
                        amount += 1;
                        
                    }
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            mouse2 = true;
        }

        private void photo_box_Click(object sender, EventArgs e)
        {
            if(mouse2)
            {
                Point site2 = photo_box.PointToClient(Control.MousePosition);
                label5.Text = site2.ToString();
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            mouse3 = true;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if(mouse3)
            {
                Point site3 = pictureBox1.PointToClient(Control.MousePosition);
                double site3x = site3.X;
                double site3y = site3.Y;
                PMAC.GetResponse(pmacNumber, "#1P", out ans1);
                PMAC.GetResponse(pmacNumber, "#2P", out ans2);
                shijiex = Math.Round(site3x * kx + Convert.ToDouble(ans1));
                shijiey = Math.Round(site3y * (-1) * ky + Convert.ToDouble(ans2));
                label13.Text = (site3x.ToString()+","+site3y.ToString()+")|(" + shijiex.ToString() + "," + shijiey.ToString() + ")");
            }
        }

        private void check_in_Click(object sender, EventArgs e)
        {
            string userName = this.textBox1.Text;
            string userPassword = this.textBox2.Text;

            if (userName.Equals("") || userPassword.Equals(""))
            {
                MessageBox.Show("用户名或密码不能为空！");
            }
            // 若不为空，验证用户名和密码是否与数据库匹配
            else
            {
                string strcon = "server=localhost;database=erjixm;uid=root;pwd=Xp344605;";
                MySqlConnection con = new MySqlConnection(strcon);
                try
                {
                    con.Open();
                    string sqlSel = "select count(*) from operator where name = '" + userName + "'and password = '" + userPassword + "'";
                    //string sqlSel = "select count(*) from mysql.user where user = '" + userName + "' and authentication_string = '" + userPassword + "'";
                    MySqlCommand com = new MySqlCommand(sqlSel, con);
                    if (Convert.ToInt32(com.ExecuteScalar()) > 0)
                    {
                        string sql = "select privilege from operator where name ='" + userName + "';";
                        MySqlCommand command = new MySqlCommand(sql, con);
                        MessageBox.Show("登录成功，欢迎用户" + userName + ",您的权限是" + command.ExecuteScalar());

                    }
                    else
                    {
                        MessageBox.Show("用户名或密码错误或未注册！");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString() + "打开数据库失败");
                }
            }
        }

        private void load_operator_Click(object sender, EventArgs e)
        {
            String connetStr = "Server=localhost;Database=erjixm;uid=root;pwd=Xp344605";
            MySqlConnection conn = new MySqlConnection(connetStr);
            try
            {
                conn.Open();
                Console.WriteLine("已经建立连接");

                string sql = "select * from operator";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader reader = cmd.ExecuteReader();//执行ExecuteReader()返回一个MySqlDataReader对象
                int ind = this.dataGridView1.RowCount;
                if (ind > 2)
                {
                    for (int i = 0; i < ind - 1; i++)
                    {
                        dataGridView1.Rows.RemoveAt(0);
                    }
                }
                while (reader.Read())
                {


                    int index = this.dataGridView1.Rows.Add();

                    this.dataGridView1.Rows[index].Cells[0].Value = reader.GetString("Name");
                    this.dataGridView1.Rows[index].Cells[1].Value = reader.GetString("Password");
                    this.dataGridView1.Rows[index].Cells[2].Value = reader.GetString("Privilege");
                }
            }
            catch (MySqlException ex)
            {
               MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private void del_operator_Click(object sender, EventArgs e)
        {
            string strcon = "server=localhost;database=erjixm;uid=root;pwd=Xp344605;";

            MySqlConnection con = new MySqlConnection(strcon);

            try
            {
                con.Open();
                List<string> selectList = new List<string>();
                for (int i = 0; i < dataGridView1.Rows.Count; i++)  //遍历datagridview所有行，看第一列“选择”是否被勾选
                {
                    if ((bool)dataGridView1.Rows[i].Cells[3].EditedFormattedValue == true) //DataGridViewCheckBoxColumn列判断是否选中
                    {
                        selectList.Add(dataGridView1.Rows[i].Cells[0].Value.ToString());//把第二列“ID”存储到selectlist列表中
                    }
                }
                for (int j = 0; j < selectList.Count; j++)  //遍历selectlist列表中的元素，将数据添加为SQL语句
                {
                    string str = selectList[j];
                    MySqlCommand cmd = new MySqlCommand("DELETE FROM  operator WHERE name='" + str + "';", con);  //sql删除选中行语句
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    dataReader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                MessageBox.Show("删除成功！！");
                con.Close();
            }
        }

        private void load_chips_Click(object sender, EventArgs e)
        {
            String connetStr = "Server=localhost;Database=erjixm;uid=root;pwd=Xp344605";
            MySqlConnection conn = new MySqlConnection(connetStr);
            try
            {
                conn.Open();
                string sql = "select * from chips";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader reader = cmd.ExecuteReader();//执行ExecuteReader()返回一个MySqlDataReader对象
                int ind = this.dataGridView2.RowCount;
                if (ind > 2)
                {
                    for (int i = 0; i < ind - 1; i++)
                    {
                        dataGridView2.Rows.RemoveAt(0);
                    }
                }
                while (reader.Read())
                {
                    int index = this.dataGridView2.Rows.Add();

                    //this.dataGridView2.Rows[index].Cells[0].Value = reader.GetString("amount");
                    this.dataGridView2.Rows[index].Cells[0].Value = reader.GetString("Chip_ID");
                    this.dataGridView2.Rows[index].Cells[1].Value = reader.GetString("XPosition");
                    this.dataGridView2.Rows[index].Cells[2].Value = reader.GetString("YPosition");

                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private void del_chips_Click(object sender, EventArgs e)
        {
            string strcon = "server=localhost;database=erjixm;uid=root;pwd=Xp344605;";

            MySqlConnection con = new MySqlConnection(strcon);

            try
            {
                con.Open();
                List<string> selectList = new List<string>();
                for (int i = 0; i < dataGridView2.Rows.Count; i++)  
                {
                    if ((bool)dataGridView2.Rows[i].Cells[3].EditedFormattedValue == true) //DataGridViewCheckBoxColumn列判断是否选中
                    {
                        selectList.Add(dataGridView2.Rows[i].Cells[0].Value.ToString());
                    }
                }
                for (int j = 0; j < selectList.Count; j++)  //遍历selectlist列表中的元素，将数据添加为SQL语句
                {
                    string str = selectList[j];
                    MySqlCommand cmd = new MySqlCommand("DELETE FROM  chips WHERE Chip_ID='" + str + "';", con);  //sql删除选中行语句
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    dataReader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                MessageBox.Show("删除成功！");
                con.Close();
            }
        }

        private void move_Click(object sender, EventArgs e)
        {
            string ans = null;
            string chip_no = ID.Text;
            string x, y;

            con_to_erjixm(chip_no);

            x = dataGridView2.Rows[0].Cells[1].Value.ToString();
            y = dataGridView2.Rows[0].Cells[2].Value.ToString();

            PMAC.GetResponse(pmacNumber, "#1J=" + x, out ans);
            PMAC.GetResponse(pmacNumber, "#2J=" + y, out ans);

        }

        private void register_Click(object sender, EventArgs e)
        {
            string userName = this.textBox1.Text;
            string userPassword = this.textBox2.Text;
            string strcon = "server=localhost;database=erjixm;uid=root;pwd=Xp344605;";

            MySqlConnection con = new MySqlConnection(strcon);
            con.Open();

            if (userName.Equals("") || userPassword.Equals(""))
            {
                MessageBox.Show("用户名或密码不能为空！");
            }
            else
            {
                string sqlSel = "select count(*) from operator where name = '" + userName + "';";
                MySqlCommand com = new MySqlCommand(sqlSel, con);
                if (Convert.ToInt32(com.ExecuteScalar()) > 0)
                {
                    MessageBox.Show("已经注册过了还注册毛啊！");
                }
                else
                {
                    String sql = "INSERT INTO operator VALUES('" + userName + "','" + userPassword + "','2')";
                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("注册成功");
                }
            }
        }
    }
}
