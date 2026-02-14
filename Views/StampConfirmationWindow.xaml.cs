using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using NfcTimeCard.Models;
using NfcTimeCard.Services;

namespace NfcTimeCard.Views
{
    public partial class StampConfirmationWindow : Window
    {
        private bool _isDuplicate = false;
        private readonly Employee _employee;
        private readonly string _stampType;
        private readonly SystemSettingModel _settings;

        public StampConfirmationWindow(Employee emp, string type, DateTime stampTime, SystemSettingModel settings)
        {
            InitializeComponent();

            _employee = emp;
            _stampType = type;
            _settings = settings;

            TxtConfirmName.Text = emp.Name;
            TxtConfirmTime.Text = stampTime.ToString("HH:mm:ss");
            TxtConfirmType.Text = type == "IN" ? "出勤" : "退勤";

            var isEntry = type == "IN";
            BadgeType.Background = new System.Windows.Media.SolidColorBrush(isEntry ? System.Windows.Media.Color.FromRgb(220, 252, 231) : System.Windows.Media.Color.FromRgb(254, 226, 226));
            TxtConfirmType.Foreground = new System.Windows.Media.SolidColorBrush(isEntry ? System.Windows.Media.Color.FromRgb(22, 101, 52) : System.Windows.Media.Color.FromRgb(153, 27, 27));

            if (emp.PhotoData != null)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    using (var ms = new MemoryStream(emp.PhotoData))
                    {
                        bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.StreamSource = ms; bitmap.EndInit();
                    }
                    ImgConfirmPhoto.Source = bitmap;
                }
                catch { }
            }

            var latest = AttendanceService.GetLatestStamp(emp.EmployeeCode);
            if (latest != null) ConfirmHistoryList.ItemsSource = new List<AttendanceLog> { latest };

            string? warning = null;
            if (latest != null)
            {
                if (latest.StampType == type && latest.WorkDate == stampTime.ToString("yyyy-MM-dd"))
                {
                    _isDuplicate = true;
                    warning = "【重複】すでに本日の記録があります。保存できません。";
                }
                else if (type == "IN" && latest.StampType == "IN") warning = "前回の退勤が記録されていません。";
                else if (type == "OUT" && latest.StampType == "OUT") warning = "出勤記録がない状態での退勤となります。";
            }

            if (!string.IsNullOrEmpty(warning))
            {
                TxtWarning.Text = warning;
                WarningPanel.Visibility = Visibility.Visible;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (_isDuplicate)
            {
                MessageBox.Show("既に記録済みのため、保存できません。打刻内容を再度確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                AttendanceService.RecordStamp(_employee, _stampType, _settings);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打刻情報の保存中にエラーが発生しました。\n{ex.Message}", "保存エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = false;
                this.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}