using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NfcTimeCard.Models;
using NfcTimeCard.Services;

namespace NfcTimeCard.Views
{
    public partial class MainWindow : System.Windows.Window
    {
        private List<Employee> _allEmployees = new();
        private const string SettingFileName = "system_setting";

        public MainWindow()
        {
            InitializeComponent();
            StartClock();
            LoadEmployees();
        }

        private void StartClock()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => {
                CurrentTimeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
                TxtDate.Text = DateTime.Now.ToString("yyyy年M月d日 (ddd)");
            };
            timer.Start();
        }

        private void LoadEmployees()
        {
            try { _allEmployees = EmployeeService.GetAllEmployees(); }
            catch (Exception ex) { MessageBox.Show($"ロード失敗: {ex.Message}"); }
        }

        private void BtnIn_Click(object sender, RoutedEventArgs e) => StartAuthAndStamp("IN");
        private void BtnOut_Click(object sender, RoutedEventArgs e) => StartAuthAndStamp("OUT");

        private void StartAuthAndStamp(string type)
        {
            var settings = GetCurrentSettings();
            if (settings == null) return;

            var authWin = new FaceRecognitionWindow(_allEmployees, type, settings);
            authWin.Owner = this; //

            // ダイアログを表示
            bool? result = authWin.ShowDialog();

            // ★重要：確認画面が閉じた後、強制的にメイン画面を最前面に持ってくる
            this.Activate();
            this.Focus();
            // 一時的にTopmostにして確実に前面へ出す（環境によって必要）
            this.Topmost = true;
            this.Topmost = false;

            if (result == true && authWin.MatchedEmployee != null)
            {
                HandleStamp(authWin.MatchedEmployee, type, settings);
            }
        }

        private void HandleStamp(Employee emp, string type, SystemSettingModel settings)
        {
            try {
                AttendanceService.RecordStamp(emp, type, settings);
                MessageBox.Show($"{emp.Name} さん、記録しました。", "完了");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "エラー"); }
        }

        private SystemSettingModel? GetCurrentSettings()
        {
            try {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingFileName);
                if (!File.Exists(path)) return null;
                byte[] decrypted = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
                return JsonSerializer.Deserialize<SystemSettingModel>(Encoding.UTF8.GetString(decrypted));
            }
            catch { return null; }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) this.DragMove(); }
        private void BtnSettings_Click(object sender, RoutedEventArgs e) { new LoginWindow().Show(); this.Close(); }
        private void BtnExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void Window_Closed(object sender, EventArgs e) { }
    }
}