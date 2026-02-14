using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using NfcTimeCard.Models;
using NfcTimeCard.Services;

namespace NfcTimeCard.Views
{
    public partial class FaceRecognitionWindow : System.Windows.Window
    {
        private VideoCapture? _capture;
        private CancellationTokenSource? _cts;
        private List<Employee> _employees;
        private string _type;
        private SystemSettingModel _settings;
        private bool _isProcessing = false;

        public Employee? MatchedEmployee { get; private set; }

        public FaceRecognitionWindow(List<Employee> employees, string type, SystemSettingModel settings)
        {
            InitializeComponent();
            _employees = employees;
            _type = type;
            _settings = settings;
            StartCamera();
        }

        private async void StartCamera()
        {
            // カメラの初期化をバックグラウンドスレッドで行うことでUIフリーズを防止
            bool initSuccess = await Task.Run(() => {
                try 
                {
                    _capture = new VideoCapture(_settings.VideoDeviceIndex);
                    return _capture.IsOpened();
                } 
                catch 
                { 
                    return false; 
                }
            });

            if (!initSuccess) { this.Close(); return; }

            _cts = new CancellationTokenSource();
            int frameCount = 0;
            try
            {
                while (_cts != null && !_cts.IsCancellationRequested)
                {
                    using var frame = new Mat();
                    
                    // キャプチャ読み取りもUIスレッドをブロックしないようにする
                    bool readSuccess = await Task.Run(() => _capture?.Read(frame) ?? false);
                    
                    if (!readSuccess || frame.Empty()) break;

                    using var cropped = CropToPortrait(frame);
                    
                    // UI更新はメインスレッドで
                    ImgPreview.Source = cropped.ToWriteableBitmap();

                    if (_isProcessing) { await Task.Delay(100); continue; }
                    if (frameCount++ % 10 == 0)
                    {
                        var found = await Task.Run(() => CheckFace(cropped));
                        if (found != null && !_isProcessing)
                        {
                            _isProcessing = true;
                            OnFaceDetected(found);
                            return;
                        }
                    }
                    await Task.Delay(33);
                }
            }
            catch { }
        }

        private async void OnFaceDetected(Employee found)
        {
            _cts?.Cancel();
            _capture?.Dispose();
            _capture = null;

            await Dispatcher.InvokeAsync(() => {
                if (!this.IsLoaded) return;

                // 確認画面の親を MainWindow (this.Owner) に設定して開く
                var confirmWin = new StampConfirmationWindow(found, _type, DateTime.Now, _settings);
                confirmWin.Owner = this.Owner; 

                // 確認画面が表示されたら、このカメラ画面をすぐに「閉じる」
                // (Visibilityを変えるだけだと、このウィンドウがフォーカスを保持しようとして邪魔をするため)
                confirmWin.Activated += (s, e) => {
                    if (this.IsLoaded) {
                        this.Hide(); // まず隠す
                    }
                };

                bool? result = confirmWin.ShowDialog();

                if (!this.IsLoaded) return;

                try {
                    MatchedEmployee = (result == true) ? found : null;
                    this.DialogResult = result;
                }
                catch { }
                this.Close();
            });
        }

        // --- 認証・比較ロジック ---
        private Employee? CheckFace(Mat frame)
        {
            // Mat (BGR) -> byte[] (RGB Raw Pixel Data) への変換 (JPEGエンコード回避)
            // ImageSharp は RGB を期待するため、BGR -> RGB 変換が必要
            using var rgbFrame = new Mat();
            Cv2.CvtColor(frame, rgbFrame, ColorConversionCodes.BGR2RGB);
            
            int width = rgbFrame.Width;
            int height = rgbFrame.Height;
            long size = rgbFrame.Total() * rgbFrame.ElemSize();
            byte[] rawPixelData = new byte[size];
            
            // ポインタ操作の代わりに安全にコピー
            System.Runtime.InteropServices.Marshal.Copy(rgbFrame.Data, rawPixelData, 0, (int)size);

            // 高速版抽出メソッドを使用
            byte[]? currentFeature = EmployeeService.ExtractFaceFeatureFromRaw(rawPixelData, width, height);

            if (currentFeature == null) return null;

            // 最も類似度が高い従業員を探す
            Employee? bestMatch = null;
            float bestScore = 0f;

            foreach (var emp in _employees)
            {
                if (emp.FaceFeature == null) continue;

                // ViewFaceCore の最適化された比較ロジックを使用
                float score = EmployeeService.CompareFaces(currentFeature, emp.FaceFeature);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = emp;
                }
            }

            // しきい値判定 (通常 0.6〜0.7 以上で同一人物とみなされることが多いですが、厳密にするなら 0.8)
            // ViewFaceCore のドキュメントでは 1:N 認証の場合 0.6 程度が目安となることもあります。
            // 誤検知を防ぐため高めに設定します。
            return (bestScore > 0.65f) ? bestMatch : null;
        }

        private Mat CropToPortrait(Mat source) { int targetWidth = (int)(source.Height * 0.75); int x = (source.Width - targetWidth) / 2; return source[new OpenCvSharp.Rect(x, 0, targetWidth, source.Height)].Clone(); }
        private void Cancel_Click(object sender, RoutedEventArgs e) { _isProcessing = true; _cts?.Cancel(); this.Close(); }
        private void Window_Closed(object sender, EventArgs e) { _cts?.Cancel(); _capture?.Dispose(); }
    }
}