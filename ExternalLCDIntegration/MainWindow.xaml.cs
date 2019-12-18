using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

namespace ExternalLCDIntegration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker _backgroundWorker;
        private bool _isRunning = false;

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
            TextBox1.Text = i.ToString();
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            DoTheLoop();
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
