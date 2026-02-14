using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NfcTimeCard.Models;
using NfcTimeCard.Services;

namespace NfcTimeCard.Views
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            ShowEmployeeList();
        }

        private void ShowEmployeeList()
        {
            var listView = new EmployeeListView();
            listView.RequestShowAttendanceDetails += (emp) =>
            {
                ShowAttendanceDetails(emp);
            };
            MainContentFrame.Content = listView;
        }

        private void ShowAttendanceDetails(Employee? target = null)
        {
            var detailsView = new AttendanceDetailsView();

            // ターゲットが指定されている場合はそれをセット
            if (target != null)
            {
                detailsView.SetTargetEmployee(target);
            }
            else
            {
                // メニューから直接来た場合など、指定がない場合はDBから一人目を取得
                var emps = EmployeeService.GetAllEmployees();
                if (emps.Count > 0)
                {
                    detailsView.SetTargetEmployee(emps[0]);
                }
            }

            MainContentFrame.Content = detailsView;
        }

        // --- サイドメニューイベント ---

        private void Menu_Employee_Click(object sender, RoutedEventArgs e)
        {
            ShowEmployeeList();
        }

        private void Menu_Attendance_Click(object sender, RoutedEventArgs e)
        {
            // 引数なしで呼び出すことで、内部ロジックにより「最初の一人」を表示
            ShowAttendanceDetails();
        }

        private void Menu_System_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Content = new SystemSettingsView();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = new MainWindow();
            mainWin.Show();
            this.Close();
        }

        // --- ウィンドウ操作 ---

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                BtnMaximize.Content = "\uE922";
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                BtnMaximize.Content = "\uE923";
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}