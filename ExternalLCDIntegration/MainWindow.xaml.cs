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
using Size = System.Drawing.Size;

namespace ExternalLCDIntegration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isRunning = false;
        private int _horizontalLedCountTop;
        private int _horizontalLedCountBottom;
        private int _verticalLedCountLeft;
        private int _verticalLedCountRight;
        private SerialPort _port;
        private readonly BackgroundWorker _backgroundWorker;
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

        private void WaitMilliseconds(int milliseconds)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < milliseconds)
            {
                Thread.Sleep(100);
            }
        }
        private void GetAverageColor()
        {
            GetScreenResolution(out var screenWidth, out var screenHeight);
            var totalLedCount = _horizontalLedCountTop 
                                + _horizontalLedCountBottom 
                                + _verticalLedCountLeft 
                                + _verticalLedCountRight;

            var size = new Size(screenWidth, screenHeight);
            do
            {
                var totals = new long[] { 0, 0, 0 };
                var array = CreateByteArray();
                WaitMilliseconds(500);
                var screenBitmap = GetScreenBitmap(screenWidth, screenHeight, size);
                var format = screenBitmap.PixelFormat;
                var bppModifier = format == System.Drawing.Imaging.PixelFormat.Format24bppRgb ? 3 : 4; // cutting corners, will fail on anything else but 32 and 24 bit images
                var sourceData = screenBitmap.LockBits(new Rectangle(0, 0, screenWidth, screenHeight), ImageLockMode.ReadOnly,
                    format);
                var stride = sourceData.Stride;
                var scan = sourceData.Scan0;

                var verticalTopBlockX = screenWidth / _horizontalLedCountTop;
                var verticalBlockY = screenHeight / 4;



                unsafe
                {
                    for (var x = 0; x < _horizontalLedCountTop; x++)
                    {
                        for (var y = 0; y < verticalBlockY; y++)
                        {
                            var startX = verticalTopBlockX*x;
                            var endX = startX + verticalTopBlockX;
                            for (var blockX = startX; blockX < endX; blockX++)
                            {

                            }
                        }
                    }
                }

                unsafe
                {
                    var p = (byte*)(void*)scan;
                    for (var y = 0; y < screenHeight; y++)
                    {
                        for (var x = 0; x < screenWidth; x++)
                        {
                            for (var color = 0; color < 3; color++)
                            {
                                var idx = y * stride + x * bppModifier + color;
                                totals[color] += p[idx];
                            }
                        }
                    }
                }

                var count = screenWidth * screenHeight;
                var avgB = GetAverageColour(totals[0], count);
                var avgG = GetAverageColour(totals[1], count);
                var avgR = GetAverageColour(totals[2], count);
                array = FillByteArray(array, avgR, avgG, avgB);
                SendDataToSerialPort(array, array.Length);
                Dispatcher.BeginInvoke(new Action(() => { PrintRGB(avgB, avgG, avgR); }));
            } while (_isRunning);
        }

        private Bitmap GetScreenBitmap(int screenWidth, int screenHeight, Size size)
        {
            var screenBitmap = new Bitmap(screenWidth, screenHeight);
            using (var g = Graphics.FromImage(screenBitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, size);
            }

            return screenBitmap;
        }

        private void GetScreenResolution(out int screenWidth, out int screenHeight)
        {
            screenWidth = (int)Math.Floor(SystemParameters.PrimaryScreenWidth);
            screenHeight = (int)Math.Floor(SystemParameters.PrimaryScreenHeight);
        }

        private byte GetAverageColour(long total, int count)
        {
            return (byte)(total / count);
        }

        private void PrintRGB(int avrB, int avrG, int avrR)
        {
            OutputBox.Text = $"R: {avrR.ToString()} G: {avrG.ToString()} B: {avrB.ToString()}";
        }



        private byte[] CreateByteArray()
        {
            var newLength = (_horizontalLedCountTop+_horizontalLedCountBottom+_verticalLedCountLeft+_verticalLedCountRight)*3;
            var arrayRGB = new byte[newLength];
            return arrayRGB;
        }

        private byte[] AddToByteArray(byte[] array, byte R, byte G, byte B, int position)
        {
            var index = position * 3;
            array[++index] = R;
            array[++index] = G;
            array[index] = B;
            return array;
        }

        private byte[] FillByteArray(byte[] array, byte R, byte G, byte B)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
            {
                array[++i] = R;
                array[++i] = G;
                array[i] = B;
            }
            return array;
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
                _horizontalLedCountTop = int.Parse(HorizontalLedCountTop.Text);
                _horizontalLedCountBottom = int.Parse(HorizontalLedCountBottom.Text);
                _verticalLedCountLeft = int.Parse(VerticalLedCountLeft.Text);
                _verticalLedCountRight = int.Parse(VerticalLedCountRight.Text);
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
