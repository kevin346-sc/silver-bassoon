using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Basler.Pylon;
using System.Drawing;
using System.Drawing.Imaging;



namespace jiemian2
{
    class Class1
    {
        //相机连接的个数
        public int CameraNumber = CameraFinder.Enumerate().Count;

        //委托+事件 = 回调函数，用于传递相机抓取的图像
        public delegate void CameraImage(Bitmap bmp);
        public event CameraImage CameraImageEvent;

        //放出一个Camera
        Camera camera;

        //basler里用于将相机采集的图像转换成位图
        PixelDataConverter pxConvert = new PixelDataConverter();

        //控制相机采集图像的过程
        bool GrabOver = false;


        //相机初始化
        public void CameraInit()
        {
            camera = new Camera();
            //自由运行模式
            camera.CameraOpened += Configuration.AcquireContinuous;

            //断开连接事件
            camera.ConnectionLost += Camera_ConnectionLost;

            //抓取开始事件
            camera.StreamGrabber.GrabStarted += StreamGrabber_GrabStarted;

            //抓取图片事件
            camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;

            //结束抓取事件
            camera.StreamGrabber.GrabStopped += StreamGrabber_GrabStopped;

            //打开相机
            camera.Open();
        }
        private void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            GrabOver = true;
        }
        private void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            IGrabResult grabResult = e.GrabResult;
            if (grabResult.IsValid)
            {
                if (GrabOver)
                    CameraImageEvent(GrabResult2Bmp(grabResult));
            }
        }

        private void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {
            GrabOver = false;
        }

        private void Camera_ConnectionLost(object sender, EventArgs e)
        {
            camera.StreamGrabber.Stop();
            DestroyCamera();
        }

        public void OneShot()
        {
            if (camera != null)
            {
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.SingleFrame);
                camera.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
        }

        public void KeepShot()
        {
            if (camera != null)
            {
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
        }

        public void Stop()
        {
            if (camera != null)
            {
                camera.StreamGrabber.Stop();
            }
        }

        //将相机抓取到的图像转换成Bitmap位图
        Bitmap GrabResult2Bmp(IGrabResult grabResult)
        {
            Bitmap b = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
            BitmapData bmpData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, b.PixelFormat);
            pxConvert.OutputPixelFormat = PixelType.BGRA8packed;
            IntPtr bmpIntpr = bmpData.Scan0;
            pxConvert.Convert(bmpIntpr, bmpData.Stride * b.Height, grabResult);
            b.UnlockBits(bmpData);
            return b;
        }

        //释放相机
        public void DestroyCamera()
        {
            if (camera != null)
            {
                camera.Close();
                camera.Dispose();
                camera = null;
            }
        }

        //设置长
        public void changWidth(int chang)
        {
            camera.Parameters[PLCamera.Width].SetValue(chang, IntegerValueCorrection.Nearest);
        }
        //设置宽
        public void kuanHeight(int kuan)
        {
            camera.Parameters[PLCamera.Height].SetValue(kuan, IntegerValueCorrection.Nearest);
        }
    }
}
