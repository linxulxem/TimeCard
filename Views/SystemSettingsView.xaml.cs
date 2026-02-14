using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management; // ビデオデバイス検出用
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NfcTimeCard.Models;
using NfcTimeCard.Services;

namespace NfcTimeCard.Views
{
    public partial class SystemSettingsView : UserControl
    {
        private const string SettingFileName = "system_setting";
        private string SettingFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingFileName);

        public SystemSettingsView()
        {
            InitializeComponent();
            InitializeDeviceList(); // ★マージ項目：デバイス一覧の初期化
            LoadSettings();
        }

        /// <summary>
        /// PCに接続されているビデオデバイスをリストアップしてComboBoxにセットします。
        /// </summary>
        private void InitializeDeviceList()
        {
            var devices = new List<VideoDeviceItem>();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')"))
                {
                    int index = 0;
                    foreach (var device in searcher.Get())
                    {
                        string name = device["Caption"]?.ToString() ?? $"Camera {index}";
                        devices.Add(new VideoDeviceItem { Index = index++, Name = name });
                    }
                }
            }
            catch 
            {
                // WMIが失敗した場合のフォールバック
                devices.Add(new VideoDeviceItem { Index = 0, Name = "デフォルトカメラ (Device 0)" });
            }

            if (devices.Count == 0)
            {
                devices.Add(new VideoDeviceItem { Index = 0, Name = "カメラが見つかりません" });
            }

            CmbVideoDevice.ItemsSource = devices;
        }

        private void LoadSettings()
        {
            if (!File.Exists(SettingFilePath))
            {
                CmbVideoDevice.SelectedIndex = 0;
                return;
            }

            try
            {
                byte[] encryptedData = File.ReadAllBytes(SettingFilePath);
                byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                string jsonString = Encoding.UTF8.GetString(decryptedData);
                var settings = JsonSerializer.Deserialize<SystemSettingModel>(jsonString);

                if (settings != null)
                {
                    TxtAdminId.Text = settings.AdminId;
                    TxtPassword.Password = settings.AdminPassword;
                    TxtDbPath.Text = settings.DbFolderPath;
                    TxtDbFileName.Text = settings.DbFileName;
                    CmbInRounding.SelectedIndex = settings.InRoundingIndex;
                    CmbOutRounding.SelectedIndex = settings.OutRoundingIndex;
                    CmbWorkRounding.SelectedIndex = settings.WorkRoundingIndex;

                    // 営業日設定の読み込み
                    CmbCutoffDay.SelectedIndex = settings.CutoffDayIndex;
                    TxtCutoffHour.Text = settings.CutoffHour.ToString();
                    TxtCutoffMinute.Text = settings.CutoffMinute.ToString();

                    // ★マージ項目：ビデオデバイス設定の反映
                    if (CmbVideoDevice.ItemsSource is List<VideoDeviceItem> devices)
                    {
                        var target = devices.FirstOrDefault(d => d.Index == settings.VideoDeviceIndex);
                        CmbVideoDevice.SelectedItem = target ?? devices.FirstOrDefault();
                    }
                }
            }
            catch { }
        }

        private void SaveInternal()
        {
            // 数値のバリデーションとクランプ処理
            int.TryParse(TxtCutoffHour.Text, out int h);
            int.TryParse(TxtCutoffMinute.Text, out int m);
            h = Math.Clamp(h, 0, 11);
            m = Math.Clamp(m, 0, 59);

            var settings = new SystemSettingModel
            {
                AdminId = TxtAdminId.Text,
                AdminPassword = TxtPassword.Password,
                DbFolderPath = TxtDbPath.Text,
                DbFileName = TxtDbFileName.Text,
                InRoundingIndex = CmbInRounding.SelectedIndex,
                OutRoundingIndex = CmbOutRounding.SelectedIndex,
                WorkRoundingIndex = CmbWorkRounding.SelectedIndex,

                // 営業日設定の保存
                CutoffDayIndex = CmbCutoffDay.SelectedIndex,
                CutoffHour = h,
                CutoffMinute = m,

                // ★マージ項目：ビデオデバイス設定の保存
                VideoDeviceIndex = (CmbVideoDevice.SelectedItem as VideoDeviceItem)?.Index ?? 0
            };

            string jsonString = JsonSerializer.Serialize(settings);
            byte[] encryptedData = ProtectedData.Protect(Encoding.UTF8.GetBytes(jsonString), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(SettingFilePath, encryptedData);
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "DB保存先フォルダを選択" };
            if (dialog.ShowDialog() == true) TxtDbPath.Text = dialog.FolderName;
        }

        private void CreateDb_Click(object sender, RoutedEventArgs e)
        {
            var verifyWin = new VerificationWindow();
            verifyWin.Owner = Window.GetWindow(this);

            if (verifyWin.ShowDialog() == true)
            {
                try
                {
                    SaveInternal();
                    DbInitializer.CreateAndInitialize(TxtDbPath.Text, TxtDbFileName.Text);
                    MessageBox.Show("設定を保存し、データベースを構築しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"DB構築エラー: {ex.Message}", "エラー");
                }
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveInternal();
                MessageBox.Show("設定情報を保存しました。", "完了");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存エラー: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ★マージ項目：ComboBox表示用の補助クラス
    /// </summary>
    public class VideoDeviceItem
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}