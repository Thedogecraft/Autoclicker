using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Autoclicker
{
    public partial class MainWindow : Window
    {
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;  
        private uint selectedHotkey = 0x77;  
        private const uint MOD_NONE = 0x0000;  

        private uint mouseButtonDown = 0x02;  
        private uint mouseButtonUp = 0x04;    

        private bool autoclickerEnabled = false;
        private DispatcherTimer autoclickerTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAutoclickerTimer();
        }

        private void InitializeAutoclickerTimer()
        {
            
            autoclickerTimer = new DispatcherTimer();
            autoclickerTimer.Interval = TimeSpan.FromMilliseconds(100); 
            autoclickerTimer.Tick += AutoclickerTimer_Tick;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            RegisterHotkey();
        }

        private void RegisterHotkey()
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            if (hWnd == IntPtr.Zero)
            {
                //MessageBox.Show("Window handle is not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            HwndSource source = HwndSource.FromHwnd(hWnd);
            source.AddHook(HwndHook); 

            if (!RegisterHotKey(hWnd, HOTKEY_ID, MOD_NONE, selectedHotkey))
            {
                //MessageBox.Show("Failed to register hotkey.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                ToggleAutoclicker();
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void ToggleAutoclicker()
        {
            autoclickerEnabled = !autoclickerEnabled;

            if (autoclickerEnabled)
            {
                StartStopButton.Content = "Stop Autoclicker";
                this.Title += " (Clicking)";
                autoclickerTimer.Start();
            }
            else
            {
                StartStopButton.Content = "Start Autoclicker";
                this.Title = "Simple Auto Clicker";
                autoclickerTimer.Stop();
            }
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleAutoclicker();
        }

        private void AutoclickerTimer_Tick(object sender, EventArgs e)
        {
            DoMouseClick(); 
        }

        private void DoMouseClick()
        {
            
            mouse_event(mouseButtonDown | mouseButtonUp, 0, 0, 0, 0);
        }

        protected override void OnClosed(EventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            if (hWnd != IntPtr.Zero)
            {
                UnregisterHotKey(hWnd, HOTKEY_ID); 
            }
            base.OnClosed(e);
        }

        private void ClickSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (autoclickerTimer != null)
            {
                autoclickerTimer.Interval = TimeSpan.FromMilliseconds(e.NewValue);
                ClickSpeedLabel.Text = $"{e.NewValue} ms";
            }
        }

        private void HotkeyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            UnregisterHotKey(hWnd, HOTKEY_ID);

            
            switch ((HotkeyComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem).Content.ToString())
            {
                case "F8":
                    selectedHotkey = 0x77;
                    break;
                case "F9":
                    selectedHotkey = 0x78;
                    break;
                case "F10":
                    selectedHotkey = 0x79;
                    break;
            }

           
            RegisterHotkey();
        }

        private void MouseButtonComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
          
            switch ((MouseButtonComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem).Content.ToString())
            {
                case "Left Click":
                    mouseButtonDown = 0x02;  
                    mouseButtonUp = 0x04;    
                    break;
                case "Right Click":
                    mouseButtonDown = 0x08;  
                    mouseButtonUp = 0x10;    
                    break;
            }
        }

        private void TextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string url = "https://github.com/thedogecraft/autoclicker";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
