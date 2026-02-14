using System;
using System.Windows;
using System.Windows.Controls;
using NfcTimeCard.Services;
using NfcTimeCard.Models;

namespace NfcTimeCard.Views
{
    public partial class EmployeeListView : UserControl
    {
        // ★重要：詳細画面への遷移を親に知らせるための信号（イベント）
        public event Action<Employee>? RequestShowAttendanceDetails;

        public EmployeeListView()
        {
            InitializeComponent();
            LoadEmployeeData();
        }

        private void LoadEmployeeData()
        {
            try
            {
                var employees = EmployeeService.GetAllEmployees();
                DgEmployees.ItemsSource = employees;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データの読み取り中にエラーが発生しました:\n{ex.Message}", "エラー");
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadEmployeeData();

        private void BtnAddEmployee_Click(object sender, RoutedEventArgs e)
        {
            var editWin = new EmployeeEditWindow();
            editWin.Owner = Window.GetWindow(this);
            if (editWin.ShowDialog() == true) LoadEmployeeData();
        }

        private void MenuEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DgEmployees.SelectedItem is Employee selected)
            {
                var editWin = new EmployeeEditWindow(selected);
                editWin.Owner = Window.GetWindow(this);
                if (editWin.ShowDialog() == true) LoadEmployeeData();
            }
        }

        private void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DgEmployees.SelectedItem is Employee selected)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"{selected.Name} さんの情報を削除してもよろしいですか？", 
                    "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        EmployeeService.DeleteEmployee(selected.EmployeeCode);
                        LoadEmployeeData(); 
                        MessageBox.Show("削除が完了しました。", "完了");
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "エラー"); }
                }
            }
        }

        /// <summary>
        /// ★ここが「打刻詳細」クリック処理：親(AdminWindow)に信号を飛ばす
        /// </summary>
        private void MenuDetails_Click(object sender, RoutedEventArgs e)
        {
            if (DgEmployees.SelectedItem is Employee selected)
            {
                // 親ウィンドウに「この人の詳細を表示して」とリクエストする
                RequestShowAttendanceDetails?.Invoke(selected);
            }
        }

        private void MenuExport_Click(object sender, RoutedEventArgs e)
        {
            if (DgEmployees.SelectedItem is Employee selected)
            {
                MessageBox.Show($"{selected.Name} さんのCSV出力機能は現在準備中です。", "情報");
            }
        }
    }
}