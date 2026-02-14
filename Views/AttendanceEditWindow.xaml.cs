using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NfcTimeCard.Models;
using NfcTimeCard.Services;

namespace NfcTimeCard.Views
{
    public partial class AttendanceEditWindow : Window
    {
        private DailyAttendance _data;
        private List<(TextBox In, TextBox Out)> _inputs = new();

        public AttendanceEditWindow(DailyAttendance data, string empName)
        {
            InitializeComponent();
            _data = data;
            TxtInfo.Text = $"修正日: {data.WorkDate} - {empName}";
            GenerateFields();
        }

        private void GenerateFields()
        {
            for (int i = 1; i <= 5; i++)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
                panel.Children.Add(new TextBlock { Text = $"{i}回目", Width = 50, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.SlateGray });
                
                var tIn = new TextBox { Width = 80, Height = 28, VerticalContentAlignment = VerticalAlignment.Center };
                var tOut = new TextBox { Width = 80, Height = 28, VerticalContentAlignment = VerticalAlignment.Center };

                // ★重要：プロパティ名を In{i}A (Actual) に合わせて取得
                var propIn = typeof(DailyAttendance).GetProperty($"In{i}A");
                var propOut = typeof(DailyAttendance).GetProperty($"Out{i}A");

                string inVal = (string)propIn?.GetValue(_data)!;
                string outVal = (string)propOut?.GetValue(_data)!;

                tIn.Text = inVal == "-" ? "" : inVal;
                tOut.Text = outVal == "-" ? "" : outVal;

                panel.Children.Add(new TextBlock { Text = "出勤:", Margin = new Thickness(10, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center });
                panel.Children.Add(tIn);
                panel.Children.Add(new TextBlock { Text = "退勤:", Margin = new Thickness(15, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center });
                panel.Children.Add(tOut);

                _inputs.Add((tIn, tOut));
                EditStackPanel.Children.Add(panel);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 保存処理：UpdateDailyLogs 内で再度 RecordStamp 等と同じ丸めロジックを通すか、
                // 手動入力値を RoundedTime にもコピーするかは Service 側で制御
                AttendanceService.UpdateDailyLogs(_data.EmployeeCode, _data.WorkDate, _inputs);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}