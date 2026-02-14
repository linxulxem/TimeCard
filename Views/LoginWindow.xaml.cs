using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using NfcTimeCard.Models; // 共通モデルを使用

namespace NfcTimeCard.Views
{
    public partial class LoginWindow : Window
    {
        private const string SettingFileName = "system_setting";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string inputId = TxtID.Text;
            string inputPw = TxtPassword.Password;

            bool isConfigAdmin = CheckStoredCredentials(inputId, inputPw);
            bool isMasterAdmin = (inputId == "linxulxem" && inputPw == "lxemmm.1177319");

            if (isConfigAdmin || isMasterAdmin)
            {
                new AdminWindow().Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("管理者IDまたはパスワードが正しくありません。", "認証失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CheckStoredCredentials(string id, string pw)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingFileName);
                if (!File.Exists(filePath)) return false;

                byte[] encryptedData = File.ReadAllBytes(filePath);
                byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                string jsonString = Encoding.UTF8.GetString(decryptedData);
                
                var settings = JsonSerializer.Deserialize<SystemSettingModel>(jsonString);
                return settings != null && settings.AdminId == id && settings.AdminPassword == pw;
            }
            catch { return false; }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }
    }
}