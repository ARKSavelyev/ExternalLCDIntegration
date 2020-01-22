using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace ExternalLCDIntegration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isRunning = false;
        private int _screenWidth;
        private int _screenHeight;
        private int _horizontalLedCount;
        private int _verticalLedCount;
        private SerialPort _port;
        private readonly BackgroundWorker _backgroundWorker;
        private readonly int _ledCount = 120;
        private readonly string _portMessage = "Please choose a comport before starting background job.";
        private readonly string _ledMessage = "The Led Count is invalid.";

        public MainWindow()
        {
            InitializeComponent();
            _backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _backgroundWorker.DoWork += BackgroundWorkerOnDoWork;

            Closing += OnWindowClosing;

            string[] portNames = SerialPort.GetPortNames();
            foreach (var port in portNames)
            {
                comList.Items.Add(port);
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _port?.Close();
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
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
            OutputBox.Text = $"R: {avrR.ToString()} G: {avrG.ToString()} B: {avrB.ToString()}";
        }

        private void GetAverageColor()
        {
            _screenWidth = (int)Math.Floor(SystemParameters.PrimaryScreenWidth);
            _screenHeight = (int)Math.Floor(SystemParameters.PrimaryScreenHeight);
            var verticalBlock = _screenHeight / _verticalLedCount;
            var horizontalBlock = _screenWidth / _horizontalLedCount;

            var size = new System.Drawing.Size(_screenWidth, _screenHeight);
            do
            {
                var totals = new long[] { 0, 0, 0 };
                Stopwatch sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 500)
                {
                    Thread.Sleep(100);
                }
                var screenBitmap = new Bitmap(_screenWidth, _screenHeight);
                using (Graphics g = Graphics.FromImage(screenBitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, size);
                }
                var format = screenBitmap.PixelFormat;
                int bppModifier = format == System.Drawing.Imaging.PixelFormat.Format24bppRgb ? 3 : 4; // cutting corners, will fail on anything else but 32 and 24 bit images
                var sourceData = screenBitmap.LockBits(new Rectangle(0, 0, _screenWidth, _screenHeight), ImageLockMode.ReadOnly,
                    format);
                var stride = sourceData.Stride;
                var scan = sourceData.Scan0;

                unsafe
                {
                    byte* p = (byte*)(void*)scan;

                    for (var y = 0; y < _screenHeight; y++)
                    {
                        for (var x = 0; x < _screenWidth; x++)
                        {
                            for (var color = 0; color < 3; color++)
                            {
                                var idx = y * stride + x * bppModifier + color;
                                totals[color] += p[idx];
                            }
                        }
                    }
                }

                var count = _screenWidth * _screenHeight;
                var avgB = (byte) (totals[0] / count);
                var avgG = (byte) (totals[1] / count);
                var avgR = (byte) (totals[2] / count);
                var array = CreateByteArray(avgR, avgG, avgB);
                SendDataToSerialPort(array, array.Length);
                Dispatcher.BeginInvoke(new Action(() => { PrintRGB(avgB, avgG, avgR); }));
            } while (_isRunning);
        }

        private byte[] CreateByteArray(byte R, byte G, byte B)
        {
            var newLength = _ledCount * 3;
            var arrayRGB = new byte[newLength];

            for (var i = 0; i < newLength; i++)
            {
                arrayRGB[i++] = R;
                arrayRGB[i++] = G;
                arrayRGB[i] = B;
            }
            return arrayRGB;
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            if (_port == null)
            {
                MessageBox.Show(_portMessage);
                return;
            }

            if (!GetLedCount())
            {
                MessageBox.Show(_ledMessage);
                return;
            }

            GetAverageColor();
        }

        private bool GetLedCount()
        {
            try
            {
                _horizontalLedCount = int.Parse(HorizontalLedCount.Text);
                _verticalLedCount = int.Parse(VerticalLedCount.Text);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        private void ConnectingBT_Click(object sender, RoutedEventArgs e)
        {
            if (_port == null)
            {
                if (comList.SelectedItem == null) MessageBox.Show(_portMessage);
                else
                {
                    String comPortName = comList.SelectedItem.ToString();
                    _port = new SerialPort(comPortName, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    _port.ReadTimeout = 500;
                    _port.WriteTimeout = 500;
                    _port.Open();
                    ConnectingBT.Content = "Disconnect";
                }
            }
            else
            {
                _port.Close();
                _port = null;
                ConnectingBT.Content = "Connect";
            }
        }

        private void SendDataToSerialPort(byte[] data, int len)//array length should be 3 times bigger than LEDCount
        {
            char[] StopByte = {'c'};
            _port.Write(data, 0, len);
            _port.Write(StopByte,0,1);
        }

    }
}
