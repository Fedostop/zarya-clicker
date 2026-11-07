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
        
        // Флаг для отслеживания зажатия клавиш (антидребезг)
        private bool _f6Pressed = false;
        private bool _f7Pressed = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // Запускаем прицел (теперь это безопасно, т.к. мы в главном потоке)
            _overlay = new CrosshairOverlay();
            _overlay.Show();

            // Запускаем сам кликер в фоне (клики можно делать из фонового потока, это нормально)
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
                // F6 (код 0x75)
                if ((GetAsyncKeyState(0x75) & 0x8000) != 0)
                {
                    if (!_f6Pressed) 
                    {
                        _enabled = !_enabled;
                        _f6Pressed = true;
                    }
                }
                else { _f6Pressed = false; }

                // F7 (код 0x76)
                if ((GetAsyncKeyState(0x76) & 0x8000) != 0)
                {
                    if (!_f7Pressed)
                    {
                        _crosshairVisible = !_crosshairVisible;
                        _overlay.Visibility = _crosshairVisible ? Visibility.Visible : Visibility.Hidden;
                        _f7Pressed = true;
                    }
                }
                else { _f7Pressed = false; }
            };
            timer.Start();
        }

        private void ClickLoop()
        {
            Random rnd = new Random();
            while (true)
            {
                // Если включено (F6) и зажата ЛКМ (0x01)
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
            // Корректно закрываем прицел, чтобы программа завершилась
            if (_overlay != null)
            {
                _overlay.Close();
            }
            base.OnClosed(e);
            Application.Current.Shutdown();
        }
    }
}
