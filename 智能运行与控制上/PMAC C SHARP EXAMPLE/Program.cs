using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.Util;
using System.Drawing;

namespace PMAC_C_SHARP_EXAMPLE
{
    static class Program
    {
        /// <summary>
        /// 上位机完成进度要求：1.实现四种运动方式（点动式、直动式、绝对式、定量式）  2.实现通讯回零调速   3.状态显示和画圆功能
        /// /// 
        /// 现已完成：实现与下位机的通讯；图形用户界面初步设计，包括所有用到的操作按钮（修改button按钮的样式）和显示框架；各轴的直动式前进和后退；读取下位机变量（M122)；
        ///          实现通讯界面与主控制页面分离，通讯后跳转；更换button按钮图案（鼠标进入、点击、离开的动态效果）；
        ///          基本实现各种按钮功能；回零按钮与下位机代码同步（缺θ轴）；位置和速度信息的实时显示；速度调节功能（部分）；下载下位机代码文件，使其在上位机操作中执行；
        ///          
        /// 待完成：画圆界面；
        ///         图形用户界面优化（更简洁美观和更易操作）；
        ///         速度调节，使每次按按钮都有不同的速度大小；
        ///         两种运动模式调试（绝对式、定量式）；
        /// </summary>

        [STAThread]
        static void Main()
        {
           /* String win1 = "Test Window";
            //新建窗口
            CvInvoke.NamedWindow(win1);
            //新建图像
            Mat img = new Mat(200, 500, DepthType.Cv8U, 3);
            //设置图像颜色
            img.SetTo(new Bgr(255, 0, 0).MCvScalar);
            //绘制文字
            CvInvoke.PutText(img, "Hello, world", new System.Drawing.Point(10, 80), FontFace.HersheyComplex, 2.0, new Bgr(0, 255, 255).MCvScalar, 4);
            //显示
            CvInvoke.Imshow(win1, img);
            CvInvoke.WaitKey(0);
            CvInvoke.DestroyWindow(win1);*/

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new DEMO());
            index index = new index();
            index.ShowDialog();

            if (index.DialogResult == DialogResult.OK)
            {
                index.Dispose();
                Application.Run(new Main());
            }
            else if (index.DialogResult == DialogResult.Cancel)
            {
                index.Dispose();
                return;
            }


        }
    }
}
