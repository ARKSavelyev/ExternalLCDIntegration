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

        private void DoTheLoop()
        {
            int i = 0;
            string test;
            do
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 500)
                {
                    Thread.Sleep(100);
                }
                i++;
                //TextBox1.Dispatcher.BeginInvoke(new Action(()=>{ TextBox1.Text = i.ToString(); }));
                Dispatcher.BeginInvoke(new Action(() => { DoShit(i); }));
                //test = TextBox1.Text;
                //TextBox1.Text = i.ToString();
            } while (_isRunning);
        }

        private void DoShit(int i)
        {
            //var screen = new Bitmap(screenHeight,);
            TextBox1.Text = $"Width: {screenWidth.ToString()} Height: {screenHeight.ToString()}";
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
                                /*
                                int idx = y * stride + x * bppModifier;
                                totals[0] += p[idx];
                                totals[1] += p[idx+1];
                                totals[2] += p[idx+2];
                                */
                            }
                        }
                    }
                }
                int avgB = (int) (totals[0] / (screenWidth * screenHeight));
                int avgG = (int) (totals[1] / (screenWidth * screenHeight));
                int avgR = (int) (totals[2] / (screenWidth * screenHeight));

                Dispatcher.BeginInvoke(new Action(() => { PrintRGB(avgB, avgG, avgR); }));
            } while (_isRunning);

            
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            GetAverageColor();
            //DoTheLoop();
            /*
            BackgroundWorker worker = (BackgroundWorker)sender;
            int i = 0;
            while (worker != null && !worker.CancellationPending)
            {
                
                Stopwatch sw = Stopwatch.StartNew();
                if (sw.ElapsedMilliseconds < 500)
                    continue;
                i++;
                Dispatcher.BeginInvoke(new Action(() => { DoShit(i); }));
            }
            */   
        }

        
    }
}
