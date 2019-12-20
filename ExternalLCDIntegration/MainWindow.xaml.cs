using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rectangle = System.Drawing.Rectangle;

namespace ExternalLCDIntegration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker _backgroundWorker;
        private bool _isRunning = false;
        private int screenWidth;
        private int screenHeight;

        public MainWindow()
        {
            InitializeComponent();
            _backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _backgroundWorker.DoWork += BackgroundWorkerOnDoWork;
        }


        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            /*
            if (_backgroundWorker == null)
            {
                _backgroundWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };
                _backgroundWorker.DoWork += BackgroundWorkerOnDoWork;
            }
            */
            if (_isRunning)
            {
                _backgroundWorker.CancelAsync();
            }
            else
            {
                _backgroundWorker.RunWorkerAsync();
            }

            _isRunning = !_isRunning;
        }

        private void PrintRGB(int avrB, int avrG, int avrR)
        {
            TextBox1.Text = $"R: {avrR.ToString()} G: {avrG.ToString()} B: {avrB.ToString()}";
        }

        private void GetAverageColor()
        {
            screenWidth = (int)Math.Floor(SystemParameters.PrimaryScreenWidth);
            screenHeight = (int)Math.Floor(SystemParameters.PrimaryScreenHeight);
            
            var totals = new long[] { 0, 0, 0 };
            do
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 500)
                {
                    Thread.Sleep(100);
                }
                var screenBitmap = new Bitmap(screenWidth, screenHeight);
                using (Graphics g = Graphics.FromImage(screenBitmap))
                {
                    //g.CopyFromScreen(());
                }
                var format = screenBitmap.PixelFormat;
                int bppModifier = format == System.Drawing.Imaging.PixelFormat.Format24bppRgb ? 3 : 4; // cutting corners, will fail on anything else but 32 and 24 bit images
                var sourceData = screenBitmap.LockBits(new Rectangle(0, 0, screenWidth, screenHeight), ImageLockMode.ReadOnly,
                    format);
                var stride = sourceData.Stride;
                var scan = sourceData.Scan0;

                unsafe
                {
                    byte* p = (byte*)(void*)scan;

                    for (int y = 0; y < screenHeight; y++)
                    {
                        for (int x = 0; x < screenWidth; x++)
                        {
                            for (int color = 0; color < 3; color++)
                            {
                                int idx = y * stride + x * bppModifier + color;
                                totals[color] += p[idx];
                            }
                        }
                    }
                }

                var count = screenWidth * screenHeight;
                int avgB = (int) (totals[0] / count);
                int avgG = (int) (totals[1] / count);
                int avgR = (int) (totals[2] / count);

                Dispatcher.BeginInvoke(new Action(() => { PrintRGB(avgB, avgG, avgR); }));
            } while (_isRunning);

            
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            GetAverageColor();
        }

    }
}
