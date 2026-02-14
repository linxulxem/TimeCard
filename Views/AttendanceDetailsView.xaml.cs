using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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