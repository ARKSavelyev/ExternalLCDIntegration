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
using System.Windows.Controls;
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
        private bool _isLEDReadingRunning;
        private bool _isLCDReadingRunning;
        private ScreenLedCountModel _screenLedCount;
        private SerialPort _ledSerialPort;
        private SerialPort _lcdSerialPort;
        private BackgroundWorker _ledBackgroundWorker;
        private BackgroundWorker _lcdBackgroundWorker;
        private readonly string _portMessage = "Please choose a comport before starting background job.";
        private readonly string _ledMessage = "The Led Count is invalid.";

        public MainWindow()
        {
            InitializeComponent();
            InitialiseLEDBackgroundWorker();
            InitialiseLCDBackgroundWorker();
            InitialiseComportComboLists();
        }

        private void InitialiseLEDBackgroundWorker()
        {
            _ledBackgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _ledBackgroundWorker.DoWork += LEDBackgroundWorkerOnDoWork;
            _ledBackgroundWorker.RunWorkerCompleted += LEDBackgroundWorkerStopWork;
            Closing += OnWindowClosing;
        }

        private void InitialiseLCDBackgroundWorker()
        {
            _lcdBackgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _lcdBackgroundWorker.DoWork += LCDBackgroundWorkerOnDoWork;
            _lcdBackgroundWorker.RunWorkerCompleted += LCDBackgroundWorkerStopWork;
            Closing += OnWindowClosing;
        }

        private void InitialiseComportComboLists()
        {
            var portNames = SerialPort.GetPortNames();
            var comboLists = new List<ComboBox> { comListLED, comListLCD };
            foreach (var comboList in comboLists)
            {
                foreach (var port in portNames)
                {
                    comboList.Items.Add(port);
                }
            }
        }

        private void LEDBackgroundWorkerStopWork(object sender, RunWorkerCompletedEventArgs e)
        {
            StartLEDButton.Content = "Start";
            StartLEDButton.IsEnabled = true;
        }

        private void LCDBackgroundWorkerStopWork(object sender, RunWorkerCompletedEventArgs e)
        {
            StartLCDButton.Content = "Start";
            StartLCDButton.IsEnabled = true;
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _ledSerialPort?.Close();
        }

        private void StartLEDButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_ledSerialPort == null || _ledSerialPort == _lcdSerialPort)
            {
                MessageBox.Show(_portMessage);
                return;
            }
            if (!GetLedCount())
            {
                MessageBox.Show(_ledMessage);
                return;
            }
            _isLEDReadingRunning = !_isLEDReadingRunning;
            if (_ledBackgroundWorker.IsBusy)
            {
                StartLEDButton.IsEnabled = false;
                StartLEDButton.Content = "Stopping...";
                _ledBackgroundWorker.CancelAsync();
            }
            else
            {
                StartLEDButton.Content = "Running";
                _ledBackgroundWorker.RunWorkerAsync();
            }
        }

        private void StartLCDButton_OnClick(object sender, RoutedEventArgs e)
        {
            /*
            if (_lcdSerialPort == null || _ledSerialPort == _lcdSerialPort)
            {
                MessageBox.Show(_portMessage);
                return;
            }
            */
            _isLCDReadingRunning = !_isLCDReadingRunning;
            if (_lcdBackgroundWorker.IsBusy)
            {
                StartLCDButton.IsEnabled = false;
                StartLCDButton.Content = "Stopping...";
                _lcdBackgroundWorker.CancelAsync();
            }
            else
            {
                StartLCDButton.Content = "Running";
                _lcdBackgroundWorker.RunWorkerAsync();
            }
        }

        private void WaitMilliseconds(int milliseconds)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < milliseconds)
            {
                Thread.Sleep(25);
            }
        }

        /// <summary>
        /// Uses dispatcher and invoker to get the value of the TextBox, no matter the thread called from.
        /// </summary>
        /// <param name="boxToRead"></param>
        /// <returns></returns>
        private string ReadTextBox(TextBox boxToRead)
        {
            return boxToRead.Dispatcher.Invoke(() => boxToRead.Text);
        }

        private void UpdateTextBox(TextBox boxToUpdate, string newText)
        {
            boxToUpdate.Dispatcher.Invoke(new Action(() =>boxToUpdate.Text = newText));
        }

        private void GetAverageColor()
        {
            ScreenService.GetScreenResolution(out var screenWidth, out var screenHeight);

            var size = new Size(screenWidth, screenHeight);

            var verticalDepth = byte.Parse(ReadTextBox(VerticalDepthSampling));
            var horizontalDepth = byte.Parse(ReadTextBox(HorizontalDepthSampling));
            var screenBitmap = ScreenService.CreateBitmap(screenWidth, screenHeight);
            var readings = ArrayService.CreateTaskByteArray(4);
            var timer = new Stopwatch();
            do
            {
                WaitMilliseconds(100);
                timer.Start();
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
                readings[readingsCount] = SendSideRequestAsync(screenHeight, screenWidth, horizontalDepth,
                    _screenLedCount.HorizontalLedCountBottom, 0, scan, bppModifier, stride, true, false, true);
                #endregion
                var resultsArray = ArrayService.AwaitTaskByteArray(readings);
                screenBitmap.UnlockBits(sourceData);
                var endArray = ArrayService.ConvertToSingleArray(resultsArray);
                var elapsedTime = timer.ElapsedMilliseconds.ToString();
                timer.Reset();
                UpdateTextBox(OutputBox1, elapsedTime);
                SendDataToSerialPort(endArray, endArray.Length);
            } while (_isLEDReadingRunning);
            screenBitmap.Dispose();
        }


        private void GetCPUTemperature()
        {
            do
            {
                WaitMilliseconds(1000);
                var cpuReadings = HardwareService.GetCPUInfo();
                UpdateTextBox(OutputBox2, string.Join(",",cpuReadings));
            } while (_isLCDReadingRunning);
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

        private void LEDBackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            GetAverageColor();
        }

        private void LCDBackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            GetCPUTemperature();
        }

        private bool GetLedCount()
        {
            try
            {
                _screenLedCount = _screenLedCount ?? new ScreenLedCountModel();
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

        

        private void ConnectLEDComport_Click(object sender, RoutedEventArgs e)
        {
            if (_ledSerialPort == null)
            {
                if (comListLED.SelectedItem == null) MessageBox.Show(_portMessage);
                else
                {
                    var comPortName = comListLED.SelectedItem.ToString();
                    if (_lcdSerialPort != null && comPortName == _lcdSerialPort.PortName)
                    {
                        MessageBox.Show(_portMessage);
                        return;
                    }
                    _ledSerialPort = new SerialPort(comPortName, 115200, System.IO.Ports.Parity.None, 8,
                        System.IO.Ports.StopBits.One) {ReadTimeout = 500, WriteTimeout = 500};
                    _ledSerialPort.Open();
                    ConnectLEDComport.Content = "Disconnect";
                }
            }
            else
            {
                _ledSerialPort.Close();
                _ledSerialPort = null;
                ConnectLEDComport.Content = "Connect";
            }
        }

        private void ConnectLCDComport_Click(object sender, RoutedEventArgs e)
        {
            if (_lcdSerialPort == null)
            {
                if (comListLED.SelectedItem == null) MessageBox.Show(_portMessage);
                else
                {
                    var comPortName = comListLED.SelectedItem.ToString();
                    if (_ledSerialPort != null && comPortName == _ledSerialPort.PortName)
                    {
                        MessageBox.Show(_portMessage);
                        return;
                    }
                    _ledSerialPort = new SerialPort(comPortName, 115200, System.IO.Ports.Parity.None, 8,
                            System.IO.Ports.StopBits.One)
                        { ReadTimeout = 500, WriteTimeout = 500 };
                    _ledSerialPort.Open();
                    ConnectLEDComport.Content = "Disconnect";
                }
            }
            else
            {
                _ledSerialPort.Close();
                _ledSerialPort = null;
                ConnectLEDComport.Content = "Connect";
            }
        }

        private void SendDataToSerialPort(byte[] data, int len)//array length should be 3 times bigger than LEDCount
        {
            char[] StopByte = {'c'};
            _ledSerialPort.Write(data, 0, len);
            _ledSerialPort.Write(StopByte,0,1);
        }

    }
}
