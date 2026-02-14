using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Text;
using Microsoft.Win32;
using NfcTimeCard.Models;
using NfcTimeCard.Services;

namespace NfcTimeCard.Views
{
    public partial class AttendanceDetailsView : UserControl
    {
        private bool _isInitialized = false;

        public AttendanceDetailsView()
        {
            InitializeComponent();
            InitializeFilters();
            _isInitialized = true;
            LoadData();
        }

        public void SetTargetEmployee(Employee target)
        {
            if (CmbEmployeeFilter.ItemsSource is List<Employee> emps)
            {
                var match = emps.FirstOrDefault(x => x.EmployeeCode == target.EmployeeCode);
                if (match != null) CmbEmployeeFilter.SelectedItem = match;
            }
        }

        private void InitializeFilters()
        {
            var emps = EmployeeService.GetAllEmployees();
            CmbEmployeeFilter.ItemsSource = emps;
            if (emps.Count > 0 && CmbEmployeeFilter.SelectedIndex == -1) CmbEmployeeFilter.SelectedIndex = 0;

            int curYear = DateTime.Now.Year;
            CmbYear.ItemsSource = Enumerable.Range(curYear - 3, 4).Reverse().ToList();
            CmbYear.SelectedItem = curYear;
            CmbMonth.ItemsSource = Enumerable.Range(1, 12).ToList();
            CmbMonth.SelectedItem = DateTime.Now.Month;
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitialized) LoadData();
        }

        private void LoadData()
        {
            if (CmbEmployeeFilter.SelectedItem is Employee emp && CmbYear.SelectedItem is int year && CmbMonth.SelectedItem is int month)
            {
                var data = AttendanceService.GetDailyAttendanceList(emp.EmployeeCode, year, month);
                GridAttendance.ItemsSource = data;

                double monthlyTotal = 0;
                foreach (var day in data)
                {
                    if (double.TryParse(day.TotalWorkTime, out double dailyHours))
                    {
                        monthlyTotal += dailyHours;
                    }
                }
                TxtMonthlyTotal.Text = monthlyTotal.ToString("0.##");
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (GridAttendance.ItemsSource is IEnumerable<DailyAttendance> items && items.Any())
            {
                string fileName = "Attendance.csv";
                if (CmbEmployeeFilter.SelectedItem is Employee emp && 
                    CmbYear.SelectedItem is int year && 
                    CmbMonth.SelectedItem is int month)
                {
                    fileName = $"{emp.EmployeeCode}_{emp.Name}_{year}_{month:D2}.csv";
                }
                else
                {
                    fileName = $"Attendance_{CmbYear.SelectedItem}_{CmbMonth.SelectedItem}.csv";
                }

                var sfd = new SaveFileDialog
                {
                    Filter = "CSVファイル (*.csv)|*.csv",
                    FileName = fileName
                };

                if (sfd.ShowDialog() == true)
                {
                    try
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("日付,合計時間,出勤1(打刻),出勤1(丸め),退勤1(打刻),退勤1(丸め),出勤2(打刻),出勤2(丸め),退勤2(打刻),退勤2(丸め),出勤3(打刻),出勤3(丸め),退勤3(打刻),退勤3(丸め),出勤4(打刻),出勤4(丸め),退勤4(打刻),退勤4(丸め),出勤5(打刻),出勤5(丸め),退勤5(打刻),退勤5(丸め)");

                        foreach (var item in items)
                        {
                            sb.AppendLine($"{item.DisplayDate},{item.TotalWorkTime},{item.In1A},{item.In1R},{item.Out1A},{item.Out1R},{item.In2A},{item.In2R},{item.Out2A},{item.Out2R},{item.In3A},{item.In3R},{item.Out3A},{item.Out3R},{item.In4A},{item.In4R},{item.Out4A},{item.Out4R},{item.In5A},{item.In5R},{item.Out5A},{item.Out5R}");
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                        MessageBox.Show("出力が完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"出力中にエラーが発生しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("出力するデータがありません。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void GridAttendance_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GridAttendance.SelectedItem is DailyAttendance selected && CmbEmployeeFilter.SelectedItem is Employee emp)
            {
                var editWin = new AttendanceEditWindow(selected, emp.Name);
                editWin.Owner = Window.GetWindow(this);
                if (editWin.ShowDialog() == true)
                {
                    LoadData();
                }
            }
        }
    }
}