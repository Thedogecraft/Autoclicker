using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using AutoUpdaterDotNET;
using System.Reflection;
using System.Threading;
using System.Net;
using Wpf.Ui.Controls;
using Wpf.Ui;
namespace Autoclicker
{
    // Windows APIs SUCK
    public partial class MainWindow
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private uint selectedHotkey = 0x77;  // the default key is F8
        private const uint MOD_NONE = 0x0000;

        // mouse button constants
        private uint mouseButtonDown = 0x02;  // left button down
        private uint mouseButtonUp = 0x04;    // left button up

        private Thread clickThread;
        private bool autoclickerEnabled = false;
        private int clickCount = 0;
        private int clickLimit = 0;
        public bool isRunning = false;

        private const double MIN_INTERVAL = 1;

        public MainWindow()
        {
            InitializeComponent();

            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.Start("https://thedogecraft.github.io/Autoclicker/Version.xml");
            AutoUpdater.RunUpdateAsAdmin = true;
            ApplicationThemeManager.Apply(this);
            SetupUIBindings();

            if (ClickSpeedSlider != null)
            {
                ClickSpeedSlider.Minimum = MIN_INTERVAL;
                ClickSpeedSlider.Maximum = 1000;
                ClickSpeedSlider.SmallChange = 10;
                ClickSpeedSlider.LargeChange = 50;
            }
        }

        private void SetupUIBindings()
        {

            StatusText.Text = "Ready - Press F8 to start";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Gray);

            // setup mouse button radio events
            LeftClickRadio.Checked += (s, e) =>
            {
                mouseButtonDown = 0x02;  // left button down
                mouseButtonUp = 0x04;    // left button up
            };

            RightClickRadio.Checked += (s, e) =>
            {
                mouseButtonDown = 0x08;  // right button down
                mouseButtonUp = 0x10;    // right button up
            };

            MiddleClickRadio.Checked += (s, e) =>
            {
                mouseButtonDown = 0x20;  // middle button down
                mouseButtonUp = 0x40;    // middle button up
            };
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
                return;
            }

            HwndSource source = HwndSource.FromHwnd(hWnd);
            source.AddHook(HwndHook);

            if (!RegisterHotKey(hWnd, HOTKEY_ID, MOD_NONE, selectedHotkey))
            {
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
                StartStopButton.Content = "Stop Clicking";
                StatusText.Text = "Running - Press hotkey to stop";
                StatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                clickCount = 0;
                isRunning = true;
                clickThread = new Thread(StartClicking);
                clickThread.IsBackground = true;
                clickThread.Start();
            }
            else
            {
                StartStopButton.Content = "Start Clicking";
                StatusText.Text = "Ready - Press hotkey to start";
                StatusIndicator.Fill = new SolidColorBrush(Colors.Gray);
                isRunning = false;
                clickThread?.Join(100);
            }
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleAutoclicker();
        }

        private void StartClicking()
        {
            while (isRunning)
            {
                DoMouseClick();
                clickCount++;
                if (clickCount >= clickLimit && clickLimit > 0)
                {
                    StopClicking();
                    Dispatcher.Invoke(() =>
                    {
                        StartStopButton.Content = "Start Clicking";
                        StatusText.Text = "Ready - Press hotkey to start";
                        StatusIndicator.Fill = new SolidColorBrush(Colors.Gray);
                        clickCount = 0;
                        autoclickerEnabled = false;
                    });
                    isRunning = false;
                    
                }
                double interval = 0;
                ClickSpeedSlider.Dispatcher.Invoke(() =>
                {
                    interval = Math.Max(MIN_INTERVAL, ClickSpeedSlider.Value);
                });
                Thread.Sleep((int)interval);
            }
        }

        public void StopClicking()
        {
            isRunning = false;
        }

        private void DoMouseClick()
        {
            mouse_event(mouseButtonDown, 0, 0, 0, 0);
            Thread.Sleep(10);
            mouse_event(mouseButtonUp, 0, 0, 0, 0);
        }

        protected override void OnClosed(EventArgs e)
        {
            isRunning = false;
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            if (hWnd != IntPtr.Zero)
            {
                UnregisterHotKey(hWnd, HOTKEY_ID);
            }
            base.OnClosed(e);
        }

        private void ClickSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = Math.Max(MIN_INTERVAL, Math.Round(e.NewValue));
           

            if (ClickSpeedLabel != null)
            {
                ClickSpeedLabel.Text = $"Clicking every {value} ms";
            }
        }

        private void HotkeyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            UnregisterHotKey(hWnd, HOTKEY_ID);

            string selectedKey = (HotkeyComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem).Content.ToString();
            switch (selectedKey)
            {
                case "F6":
                    selectedHotkey = 0x75;
                    break;
                case "F7":
                    selectedHotkey = 0x76;
                    break;
                case "F8":
                    selectedHotkey = 0x77;
                    break;
                case "F9":
                    selectedHotkey = 0x78;
                    break;
                case "F10":
                    selectedHotkey = 0x79;
                    break;
                case "F11":
                    selectedHotkey = 0x7A;
                    break;
                case "F12":
                    selectedHotkey = 0x7B;
                    break;
            }

            RegisterHotkey();
            StatusText.Text = $"Ready - Press {selectedKey} to start";
        }
        private async void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.Error == null)
            {
                if (args.IsUpdateAvailable)
                {
                    var messageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "Update Available",
                        Content = args.Mandatory.Value
                            ? $"A new version {args.CurrentVersion} is available. You are using version {args.InstalledVersion}. This is a required update. Press OK to begin updating."
                            : $"A new version {args.CurrentVersion} is available. You are using version {args.InstalledVersion}. Do you want to update now?",
                        PrimaryButtonText = args.Mandatory.Value ? "OK" : "Yes",
                        SecondaryButtonText = args.Mandatory.Value ? null : "No",
                        CloseButtonText = "Cancel",
                        ShowTitle = true
                    };

                    var result = await messageBox.ShowDialogAsync();

                    if (result == Wpf.Ui.Controls.MessageBoxResult.Primary) // User clicked 'Yes' or 'OK'
                    {
                        try
                        {
                            if (AutoUpdater.DownloadUpdate(args))
                            {
                                Application.Current.Shutdown();
                            }
                        }
                        catch (Exception exception)
                        {
                            await ShowErrorDialog(exception);
                        }
                    }
                }
                else
                {
                    await ShowMessageDialog("No Update Available", "There is no update available. Please try again later.");
                }
            }
            else
            {
                string errorMessage = args.Error is WebException
                    ? "There is a problem reaching the update server. Please check your internet connection and try again later."
                    : args.Error.Message;

                await ShowMessageDialog("Update Check Failed", errorMessage);
            }
        }

        private async Task ShowMessageDialog(string title, string message)
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "OK",
                ShowTitle = true
            };

            await messageBox.ShowDialogAsync();
        }

        private async Task ShowErrorDialog(Exception exception)
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = exception.GetType().ToString(),
                Content = exception.Message,
                PrimaryButtonText = "OK",
                ShowTitle = true
            };

            await messageBox.ShowDialogAsync();
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

        private void ClickLimitBox_ValueChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            clickLimit = (int)args.NewValue;
        }
    }
}