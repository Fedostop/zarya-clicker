using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ZaryaClick
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        private bool _enabled = false;
        private bool _crosshairVisible = true;
        private CrosshairOverlay _overlay;
        private int _cps = 12;
        private int _jitter = 5;

        public MainWindow()
        {
            InitializeComponent();
            
            // Запускаем прицел
            _overlay = new CrosshairOverlay();
            _overlay.Show();

            // Запускаем кликер в фоне
            new Thread(ClickLoop) { IsBackground = true }.Start();
            
            // Запускаем проверку хоткеев
            StartHotkeyListener();
        }

        private void StartHotkeyListener()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += (s, e) =>
            {
                // F6 — вкл/выкл кликер
                if ((GetAsyncKeyState(0x75) & 0x8000) != 0)
                {
                    _enabled = !_enabled;
                    Thread.Sleep(200);
                }
                
                // F7 — вкл/выкл прицел
                if ((GetAsyncKeyState(0x76) & 0x8000) != 0)
                {
                    _crosshairVisible = !_crosshairVisible;
                    _overlay.Visibility = _crosshairVisible ? Visibility.Visible : Visibility.Hidden;
                    Thread.Sleep(200);
                }
            };
            timer.Start();
        }

        private void ClickLoop()
        {
            Random rnd = new Random();
            while (true)
            {
                // Если включено и зажата ЛКМ
                if (_enabled && (GetAsyncKeyState(0x01) & 0x8000) != 0)
                {
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                    int delay = 1000 / _cps;
                    if (_jitter > 0)
                    {
                        int range = delay * _jitter / 100;
                        delay += rnd.Next(-range, range + 1);
                    }
                    Thread.Sleep(Math.Max(1, delay));
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
        }

        private void BtnCrosshair_Click(object sender, RoutedEventArgs e)
        {
            _crosshairVisible = !_crosshairVisible;
            _overlay.Visibility = _crosshairVisible ? Visibility.Visible : Visibility.Hidden;
            BtnCrosshair.Content = _crosshairVisible ? "Скрыть прицел" : "Показать прицел";
        }

        protected override void OnClosed(EventArgs e)
        {
            _overlay?.Close();
            base.OnClosed(e);
            Application.Current.Shutdown();
        }
    }
}