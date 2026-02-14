using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace NfcTimeCard.Views
{
    /// <summary>
    /// System.Windows.Window を明示的に指定して衝突を回避します。
    /// </summary>
    public partial class CameraCaptureWindow : System.Windows.Window
    {
        private VideoCapture? _capture;
        private CancellationTokenSource? _cts;
        public string? CapturedImagePath { get; private set; }

        public CameraCaptureWindow()
        {
            InitializeComponent();
            StartCamera();
        }

        private async void StartCamera()
        {
            _capture = new VideoCapture(0); 
            
            if (!_capture.IsOpened())
            {
                MessageBox.Show("カメラを起動できませんでした。");
                this.Close();
                return;
            }

            _cts = new CancellationTokenSource();
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    using var frame = new Mat();
                    _capture.Read(frame);

                    if (!frame.Empty())
                    {
                        // 3:4 の比率で中央をクロップして表示
                        using var cropped = CropToPortrait(frame);
                        ImgPreview.Source = cropped.ToWriteableBitmap();
                    }
                    await Task.Delay(33);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Camera Error: {ex.Message}");
            }
        }

        /// <summary>
        /// OpenCvSharp.Rect を明示的に使用して切り抜きを行います。
        /// </summary>
        private Mat CropToPortrait(Mat source)
        {
            // 高さを基準に、幅を 3/4 (0.75) に計算
            int targetWidth = (int)(source.Height * 0.75);
            
            if (targetWidth > source.Width) targetWidth = source.Width;

            int x = (source.Width - targetWidth) / 2;

            // ★修正ポイント: OpenCvSharp.Rect と明示し、indexer ([]) で切り出す
            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(x, 0, targetWidth, source.Height);
            
            // source[roi] は参照なので、Clone() して新しい Mat として返します
            return source[roi].Clone();
        }

        private void Capture_Click(object sender, RoutedEventArgs e)
        {
            if (_capture == null || !_capture.IsOpened()) return;

            using var frame = new Mat();
            _capture.Read(frame);

            if (!frame.Empty())
            {
                using var cropped = CropToPortrait(frame);
                
                string tempFile = Path.Combine(Path.GetTempPath(), $"face_capture_{Guid.NewGuid()}.jpg");
                cropped.SaveImage(tempFile);

                this.CapturedImagePath = tempFile;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _cts?.Cancel();
            _capture?.Dispose();
        }
    }
}