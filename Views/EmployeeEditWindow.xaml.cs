using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NfcTimeCard.Models;
using NfcTimeCard.Services;

namespace NfcTimeCard.Views
{
    public partial class EmployeeEditWindow : Window
    {
        private bool _isEditMode = false;
        private byte[]? _photoData = null;

        public EmployeeEditWindow(Employee? targetEmployee = null)
        {
            InitializeComponent();

            if (targetEmployee != null)
            {
                _isEditMode = true;
                this.Title = "従業員情報の編集";
                TxtCode.Text = targetEmployee.EmployeeCode;
                TxtCode.IsEnabled = false;
                TxtName.Text = targetEmployee.Name;
                TxtAddress.Text = targetEmployee.Address;
                TxtNfcId.Text = targetEmployee.NfcId;

                foreach (ComboBoxItem item in CmbGender.Items)
                {
                    if (item.Content.ToString() == targetEmployee.Gender) { CmbGender.SelectedItem = item; break; }
                }

                if (targetEmployee.PhotoData != null)
                {
                    _photoData = targetEmployee.PhotoData;
                    // XAML側の名前に合わせ「ImgPhoto」を使用
                    ImgPhoto.Source = BytesToImage(_photoData);
                }
            }
        }

        private void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "画像ファイル|*.jpg;*.jpeg;*.png" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // サービス層でリサイズ処理
                    _photoData = EmployeeService.GetResizedImageBytes(dialog.FileName);
                    ImgPhoto.Source = BytesToImage(_photoData);
                }
                catch (Exception ex) { MessageBox.Show($"画像処理エラー: {ex.Message}"); }
            }
        }

        private void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            var cameraWin = new CameraCaptureWindow();
            cameraWin.Owner = this;

            if (cameraWin.ShowDialog() == true)
            {
                try
                {
                    string? capturedPath = cameraWin.CapturedImagePath;

                    if (!string.IsNullOrEmpty(capturedPath) && File.Exists(capturedPath))
                    {
                        // サービス層でリサイズ処理を実行
                        _photoData = EmployeeService.GetResizedImageBytes(capturedPath);

                        // 画像を表示
                        ImgPhoto.Source = BytesToImage(_photoData);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"画像の取り込みに失敗しました: {ex.Message}");
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCode.Text) || string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("従業員コードと氏名は必須入力です。"); return;
            }

            try
            {
                byte[]? features = null;
                if (_photoData != null)
                {
                    // 保存ボタン押下時に特徴量を抽出
                    features = EmployeeService.ExtractFaceFeature(_photoData);

                    if (features == null)
                    {
                        // 顔が見つからない場合の警告
                        var result = MessageBox.Show(
                            "画像から顔を正しく認識できませんでした。このまま保存しますか？\n(顔認証機能が利用できなくなります)",
                            "顔認識エラー", 
                            MessageBoxButton.YesNo, 
                            MessageBoxImage.Warning);
                        
                        if (result == MessageBoxResult.No) return;
                    }
                }

                var emp = new Employee {
                    EmployeeCode = TxtCode.Text,
                    Name = TxtName.Text,
                    Gender = (CmbGender.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "",
                    Address = TxtAddress.Text,
                    NfcId = TxtNfcId.Text,
                    PhotoData = _photoData,
                    FaceFeature = features // 抽出した数値データをセット
                };

                // 保存処理をサービスへ委譲
                EmployeeService.SaveEmployee(emp, _isEditMode);
                
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show($"保存エラー: {ex.Message}"); }
        }

        private BitmapImage BytesToImage(byte[] data)
        {
            var bitmap = new BitmapImage();
            using (var ms = new MemoryStream(data))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
            }
            return bitmap;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}