using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using ExternalLCDIntegration.Extensions;
using ExternalLCDIntegration.Models;
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
        private ScreenLedCountModel _screenLedCount;
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

            var size = new Size(screenWidth, screenHeight);

            var verticalDepth = byte.Parse(VerticalDepthSampling.Text);
            var horizontalDepth = byte.Parse(HorizontalDepthSampling.Text);
            var screenBitmap = ScreenService.CreateBitmap(screenWidth, screenHeight);
            var readings = new Task<byte[]>[4];
            Task.WaitAll(readings);
            do
            {
                WaitMilliseconds(100);
                screenBitmap = ScreenService.CopyFromTheScreen(screenBitmap, size);
                var format = screenBitmap.PixelFormat;
                var bppModifier =
                    format == PixelFormat.Format24bppRgb
                        ? 3
                        : 4; // cutting corners, will fail on anything else but 32 and 24 bit images
                var sourceData = screenBitmap.LockBits(new Rectangle(0, 0, screenWidth, screenHeight),
                    ImageLockMode.ReadOnly,
                    format);
                var stride = sourceData.Stride;
                var scan = sourceData.Scan0;
                var readingsCount = 0;
               
                #region VerticalRight
                readings[readingsCount++] = SendSideRequestAsync(screenHeight, screenWidth, verticalDepth,
                    _screenLedCount.VerticalLedCountRight, 0, scan, bppModifier, stride, false, false, false);
                #endregion

                #region HorizonalTop
                readings[readingsCount++] = SendSideRequestAsync(screenHeight, screenWidth, horizontalDepth,
                    _screenLedCount.HorizontalLedCountTop, 0, scan, bppModifier, stride, false, true, true);
                #endregion

                #region VerticalLeft
                readings[readingsCount++] = SendSideRequestAsync(screenHeight, screenWidth, verticalDepth,
                    _screenLedCount.VerticalLedCountLeft, 0, scan, bppModifier, stride, true, true, false);
                #endregion

                #region HorizonalBottom
                readings[readingsCount++] = SendSideRequestAsync(screenHeight, screenWidth, horizontalDepth,
                    _screenLedCount.HorizontalLedCountBottom, 0, scan, bppModifier, stride, true, false, true);
                #endregion

                /* SYNCHRONOUS VERSION
                var requestModel = new SideLedReadingRequest
                {
                    Y = screenHeight,
                    X = screenWidth,
                    Depth = verticalDepth,
                    SideLedCount = _screenLedCount.VerticalLedCountRight,
                    CurrentLedCount = 0,
                    ScreenPointer = scan,
                    BPPModifier = bppModifier,
                    Stride = stride,
                    ColourArray = ArrayService.CreateByteArray(_screenLedCount),
                    IsIncremental = false,
                    StartFromZero = false
                };

                #region VerticalRight
                
                requestModel.ColourArray = ScreenService.GetSideLED(requestModel);
                #endregion

                #region HorizonalTop
                requestModel.StartFromZero = true;
                requestModel.SideLedCount = _screenLedCount.HorizontalLedCountTop;
                requestModel.IsHorizontal = true;
                requestModel.Depth = horizontalDepth;
                requestModel.ColourArray = ScreenService.GetSideLED(requestModel);
                #endregion

                #region VerticalLeft
                requestModel.SideLedCount = _screenLedCount.VerticalLedCountLeft;
                requestModel.IsHorizontal = false;
                requestModel.IsIncremental = true;
                requestModel.Depth = verticalDepth;
                requestModel.ColourArray = ScreenService.GetSideLED(requestModel);
                #endregion

                #region HorizonalBottom
                requestModel.StartFromZero = false;
                requestModel.SideLedCount = _screenLedCount.HorizontalLedCountBottom;
                requestModel.IsHorizontal = true;
                requestModel.Depth = horizontalDepth;
                requestModel.ColourArray = ScreenService.GetSideLED(requestModel);
                #endregion
                */
                var resultsArray = new byte[readingsCount][];
                for (var loopCount = 0; loopCount < readingsCount; loopCount++)
                {
                    resultsArray[loopCount] = readings[loopCount].Result;
                }

                screenBitmap.UnlockBits(sourceData);
                var endArray = ArrayService.ConvertToSingleArray(resultsArray);
                SendDataToSerialPort(endArray, endArray.Length);
                //Dispatcher.BeginInvoke(new Action(() => { PrintRGB(avgB, avgG, avgR); }));
            } while (_isRunning);
        }


        private Task<byte[]> SendSideRequestAsync(int screenHeight, int screenWidth, byte depth, byte sideLedCount, int currentLedCount, IntPtr screenPointer, int bppModifier, int stride, bool isIncremental, bool startFromZero, bool isHorizontal) 
        {
            var requestModel = new SideLedReadingRequest
            {
                Y = screenHeight,
                X = screenWidth,
                Depth = depth,
                SideLedCount = sideLedCount,
                CurrentLedCount = currentLedCount,
                ScreenPointer = screenPointer,
                BPPModifier = bppModifier,
                Stride = stride,
                ColourArray = ArrayService.CreateByteArray(_screenLedCount),
                IsIncremental = isIncremental,
                StartFromZero = startFromZero,
                IsHorizontal = isHorizontal
            };
            return ScreenService.GetSideLEDAsync(requestModel);
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            GetAverageColor();
        }

        private bool GetLedCount()
        {
            try
            {
                if (_screenLedCount == null)
                {
                    _screenLedCount = new ScreenLedCountModel();
                }
                _screenLedCount.HorizontalLedCountTop = byte.Parse(HorizontalLedCountTop.Text);
                _screenLedCount.HorizontalLedCountBottom = byte.Parse(HorizontalLedCountBottom.Text);
                _screenLedCount.VerticalLedCountLeft = byte.Parse(VerticalLedCountLeft.Text);
                _screenLedCount.VerticalLedCountRight = byte.Parse(VerticalLedCountRight.Text);
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
                    var comPortName = comList.SelectedItem.ToString();
                    _port = new SerialPort(comPortName, 115200, System.IO.Ports.Parity.None, 8,
                        System.IO.Ports.StopBits.One) {ReadTimeout = 500, WriteTimeout = 500};
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
