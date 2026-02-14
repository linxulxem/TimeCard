using System;
using System.Windows;

namespace NfcTimeCard.Views
{
    public partial class VerificationWindow : Window
    {
        private string _targetCode;

        public VerificationWindow()
        {
            InitializeComponent();
            // 4桁のランダム数値を生成
            var random = new Random();
            _targetCode = random.Next(1000, 9999).ToString();
            TxtVerifyCode.Text = _targetCode;
            InputCodeField.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (InputCodeField.Text == _targetCode)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("確認コードが一致しません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}