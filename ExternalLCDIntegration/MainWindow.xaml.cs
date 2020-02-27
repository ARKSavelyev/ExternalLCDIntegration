using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using ExternalLCDIntegration.Extensions;
using ExternalLCDIntegration.Services;
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
        private int _horizontalLedCountTop = 5;
        private int _horizontalLedCountBottom = 5;
        private int _verticalLedCountLeft = 5;
        private int _verticalLedCountRight = 5;
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
                Thread.Sleep(25);
            }
        }
        private void GetAverageColor()
        {
            ScreenService.GetScreenResolution(out var screenWidth, out var screenHeight);
            var totalLedCount = _horizontalLedCountTop 
                                + _horizontalLedCountBottom 
                                + _verticalLedCountLeft 
                                + _verticalLedCountRight;

            var size = new Size(screenWidth, screenHeight);
            do
            {
                var totals = new long[] { 0, 0, 0 };
                var array = ArrayService.CreateByteArray(_horizontalLedCountTop,_horizontalLedCountBottom, _verticalLedCountLeft, _verticalLedCountRight);
                var ledCount = 0;
                WaitMilliseconds(500);
                var screenBitmap = ScreenService.GetScreenBitmap(screenWidth, screenHeight, size);
                var format = screenBitmap.PixelFormat;
                var bppModifier = format == PixelFormat.Format24bppRgb ? 3 : 4; // cutting corners, will fail on anything else but 32 and 24 bit images
                var sourceData = screenBitmap.LockBits(new Rectangle(0, 0, screenWidth, screenHeight), ImageLockMode.ReadOnly,
                    format);
                var stride = sourceData.Stride;
                var scan = sourceData.Scan0;
                unsafe
                {
                    var p = (byte*)(void*)scan;
                    var pixelCount = 0;

                    #region HorizonalTop

                    var horizonalBlockX = screenWidth / _horizontalLedCountTop;
                    var horizontalBlockY = screenHeight / 5;
                    for (var x = 0; x < _horizontalLedCountTop-1; x++)
                    {
                        for (var y = 0; y < horizontalBlockY; y++)
                        {
                            var startX = horizonalBlockX*x;
                            var endX = startX + horizonalBlockX;
                            for (var blockX = startX; blockX < endX; blockX++)
                            {
                                for (var color = 0; color < 3; color++)
                                {
                                    var idx = y * stride + blockX * bppModifier + color;
                                    totals[color] += p[idx];
                                }
                                pixelCount++;
                            }
                        }
                        array = AddLedColourToArray(totals, array, pixelCount, ledCount++);
                        totals.CleanArray();
                    }
                    pixelCount = 0;
                    for (var y = 0; y < horizontalBlockY; y++)
                    {
                        var startX = horizonalBlockX * _horizontalLedCountTop - 1;
                        for (var blockX = startX; blockX < screenWidth; blockX++)
                        {
                            for (var color = 0; color < 3; color++)
                            {
                                var idx = y * stride + blockX * bppModifier + color;
                                totals[color] += p[idx];
                            }
                            pixelCount++;
                        }
                    }
                    array = AddLedColourToArray(totals, array, pixelCount, ledCount++);
                    totals.CleanArray();
                    #endregion

                    #region VerticalLeft

                    var VerticalBlockY = screenHeight / _verticalLedCountLeft;
                    var VerticalBlockX = screenWidth / 5;
                    for (var y = 0; y < _verticalLedCountLeft-1; y++)
                    {
                        for (var x = 0; x < VerticalBlockX; x++)
                        {
                            var startY = VerticalBlockY * y;
                            var endY = startY + VerticalBlockY;
                            for (var blockY = startY; blockY < endY; blockY++)
                            {
                                for (var color = 0; color < 3; color++)
                                {
                                    var idx = blockY * stride + x * bppModifier + color;
                                    totals[color] += p[idx];
                                }
                                pixelCount++;
                            }
                        }
                        array = AddLedColourToArray(totals, array, pixelCount, ledCount++);
                        totals.CleanArray();
                    }
                    pixelCount = 0;
                    for (var x = 0; x < VerticalBlockX; x++)
                    {
                        var startY = VerticalBlockY * _verticalLedCountLeft - 1;
                        for (var blockY = startY; blockY < screenHeight; blockY++)
                        {
                            for (var color = 0; color < 3; color++)
                            {
                                var idx = blockY * stride + x * bppModifier + color;
                                totals[color] += p[idx];
                            }
                            pixelCount++;
                        }
                    }
                    array = AddLedColourToArray(totals, array, pixelCount, ledCount++);
                    totals.CleanArray();
                    #endregion

                    #region VerticalRight

                    VerticalBlockY = screenHeight / _verticalLedCountRight;
                    for (var y = 0; y < _verticalLedCountRight - 1; y++)
                    {
                        for (var x = screenWidth- VerticalBlockX; x < screenWidth; x++)
                        {
                            var startY = VerticalBlockY * y;
                            var endY = startY + VerticalBlockY;
                            for (var blockY = startY; blockY < endY; blockY++)
                            {
                                for (var color = 0; color < 3; color++)
                                {
                                    var idx = blockY * stride + x * bppModifier + color;
                                    totals[color] += p[idx];
                                }
                                pixelCount++;
                            }
                        }
                        array = AddLedColourToArray(totals, array, pixelCount, ledCount++);
                        totals.CleanArray();
                    }
                    pixelCount = 0;
                    for (var x = screenWidth - VerticalBlockX; x < screenWidth; x++)
                    {
                        var startY = VerticalBlockY * _verticalLedCountRight - 1;
                        for (var blockY = startY; blockY < screenHeight; blockY++)
                        {
                            for (var color = 0; color < 3; color++)
                            {
                                var idx = blockY * stride + x * bppModifier + color;
                                totals[color] += p[idx];
                            }

                            pixelCount++;
                        }
                    }

                    array = AddLedColourToArray(totals, array, pixelCount, ledCount++);
                    totals.CleanArray();
                    #endregion

                    #region HorizonalBottom

                    horizonalBlockX = screenWidth / _horizontalLedCountBottom;
                    horizontalBlockY = screenHeight / 5;
                    for (var x = 0; x < _horizontalLedCountBottom - 1; x++)
                    {
                        for (var y = screenHeight-horizontalBlockY; y < screenHeight; y++)
                        {
                            var startX = horizonalBlockX * x;
                            var endX = startX + horizonalBlockX;
                            for (var blockX = startX; blockX < endX; blockX++)
                            {
                                for (var color = 0; color < 3; color++)
                                {
                                    var idx = y * stride + blockX * bppModifier + color;
                                    totals[color] += p[idx];
                                }
                                pixelCount++;
                            }
                        }
                        array = AddLedColourToArray(totals, array, pixelCount, ledCount++);
                        totals.CleanArray();
                    }
                    pixelCount = 0;
                    for (var y = 0; y < horizontalBlockY; y++)
                    {
                        var startX = horizonalBlockX * _horizontalLedCountTop - 1;
                        for (var blockX = startX; blockX < screenWidth; blockX++)
                        {
                            for (var color = 0; color < 3; color++)
                            {
                                var idx = y * stride + blockX * bppModifier + color;
                                totals[color] += p[idx];
                            }
                            pixelCount++;
                        }
                    }
                    array = AddLedColourToArray(totals, array, pixelCount, ledCount++);
                    totals.CleanArray();
                    #endregion
                }

                SendDataToSerialPort(array, array.Length);
                //Dispatcher.BeginInvoke(new Action(() => { PrintRGB(avgB, avgG, avgR); }));
            } while (_isRunning);
        }

        private byte[] AddLedColourToArray(long[] colourArray, byte[] outputArray, int pixelCount, int currentLedCount)
        {
            var avgB = ScreenService.GetAverageColour(colourArray[0], pixelCount);
            var avgG = ScreenService.GetAverageColour(colourArray[1], pixelCount);
            var avgR = ScreenService.GetAverageColour(colourArray[2], pixelCount);
            return ArrayService.AddToByteArray(outputArray, avgR, avgG, avgB, currentLedCount);
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
                /*
                _horizontalLedCountTop = int.Parse(HorizontalLedCountTop.Text);
                _horizontalLedCountBottom = int.Parse(HorizontalLedCountBottom.Text);
                _verticalLedCountLeft = int.Parse(VerticalLedCountLeft.Text);
                _verticalLedCountRight = int.Parse(VerticalLedCountRight.Text);
                */
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
