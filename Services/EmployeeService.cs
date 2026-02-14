using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Media.Imaging;
using Microsoft.Data.Sqlite;
using NfcTimeCard.Models;
// ViewFaceCore 4.x の名前空間
using ViewFaceCore;
using ViewFaceCore.Core;
using ViewFaceCore.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NfcTimeCard.Services
{
    public static class EmployeeService
    {
        private const string SettingFileName = "system_setting";

        // ViewFaceCore 0.3.8 API
        private static readonly FaceDetector _faceDetector = new FaceDetector();
        private static readonly FaceLandmarker _faceLandmarker = new FaceLandmarker(); // Reverted to FaceLandmarker
        private static readonly FaceRecognizer _faceRecognizer = new FaceRecognizer();

        public static List<Employee> GetAllEmployees()
        {
            var employees = new List<Employee>();
            string dbPath = GetDatabasePath();
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath)) return employees;

            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                using (var cmd = new SqliteCommand("SELECT * FROM Employees ORDER BY EmployeeCode ASC", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        employees.Add(new Employee {
                            Id = r.GetInt32(0),
                            EmployeeCode = r.GetString(1),
                            Name = r.GetString(2),
                            Gender = r.IsDBNull(3) ? "" : r.GetString(3),
                            Address = r.IsDBNull(4) ? "" : r.GetString(4),
                            NfcId = r.IsDBNull(5) ? "" : r.GetString(5),
                            PhotoData = r.IsDBNull(6) ? null : (byte[])r["PhotoData"],
                            FaceFeature = r.IsDBNull(7) ? null : (byte[])r["FaceFeature"]
                        });
                    }
                }
            }
            return employees;
        }

        public static void SaveEmployee(Employee emp, bool isEditMode)
        {
            string dbPath = GetDatabasePath();
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = isEditMode
                    ? "UPDATE Employees SET Name=@n, Gender=@g, Address=@a, NfcId=@i, PhotoData=@p, FaceFeature=@f WHERE EmployeeCode=@c"
                    : "INSERT INTO Employees (EmployeeCode, Name, Gender, Address, NfcId, PhotoData, FaceFeature) VALUES (@c, @n, @g, @a, @i, @p, @f)";

                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@n", emp.Name);
                    cmd.Parameters.AddWithValue("@g", emp.Gender);
                    cmd.Parameters.AddWithValue("@a", emp.Address);
                    cmd.Parameters.AddWithValue("@i", emp.NfcId);
                    cmd.Parameters.AddWithValue("@c", emp.EmployeeCode);
                    cmd.Parameters.AddWithValue("@p", emp.PhotoData ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@f", emp.FaceFeature ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static byte[] GetResizedImageBytes(string filePath)
        {
            BitmapImage original = new BitmapImage();
            using (var stream = File.OpenRead(filePath))
            {
                original.BeginInit();
                original.CacheOption = BitmapCacheOption.OnLoad;
                original.StreamSource = stream;
                original.EndInit();
            }

            double maxSide = Math.Max(original.PixelWidth, original.PixelHeight);
            double scale = maxSide > 800 ? 800.0 / maxSide : 1.0;

            BitmapSource source = original;
            if (scale < 1.0)
            {
                source = new TransformedBitmap(original, new System.Windows.Media.ScaleTransform(scale, scale));
            }

            using (var ms = new MemoryStream())
            {
                var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 最新版(0.6.x)対応の特徴抽出処理
        /// </summary>
        public static byte[]? ExtractFaceFeature(byte[] imageBytes)
        {
            try
            {
                using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imageBytes);
                return ExtractFromImage(image);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Face Extraction Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 生ピクセルデータ(RGB24)から特徴抽出を行う（高速化用）
        /// </summary>
        public static byte[]? ExtractFaceFeatureFromRaw(byte[] rgbData, int width, int height)
        {
            try
            {
                using var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgb24>(rgbData, width, height);
                return ExtractFromImage(image);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Face Extraction From Raw Error: {ex.Message}");
                return null;
            }
        }

        private static byte[]? ExtractFromImage(Image<Rgb24> image)
        {
            // 1. 顔を検出
            var faces = _faceDetector.Detect(image);
            if (faces == null || faces.Length == 0) return null;

            // スコアの最も高い顔を選択
            var mainFace = faces.OrderByDescending(f => f.Score).First();

            // 2. 顔の目印(Landmark)を特定 (特徴抽出にはこれが必要)
            var points = _faceLandmarker.Mark(image, mainFace);

            // 3. 特徴量(Embeddings)を抽出
            float[] features = _faceRecognizer.Extract(image, points);

            // 4. 保存用にバイナリ化
            byte[] result = new byte[features.Length * sizeof(float)];
            Buffer.BlockCopy(features, 0, result, 0, result.Length);
                
            return result;
        }

        /// <summary>
        /// 2つの顔特徴量を比較し、類似度(0.0〜1.0)を返します。
        /// ViewFaceCore 0.3.8 互換のため手動でコサイン類似度を計算します。
        /// </summary>
        public static float CompareFaces(byte[] featureBytes1, byte[] featureBytes2)
        {
            if (featureBytes1 == null || featureBytes2 == null) return 0f;

            float[] f1 = ToFloatArray(featureBytes1);
            float[] f2 = ToFloatArray(featureBytes2);

            if (f1.Length != f2.Length) return 0f;

            float dot = 0, mag1 = 0, mag2 = 0;
            for (int i = 0; i < f1.Length; i++)
            {
                dot += f1[i] * f2[i];
                mag1 += f1[i] * f1[i];
                mag2 += f2[i] * f2[i];
            }

            if (mag1 == 0 || mag2 == 0) return 0f;

            return dot / ((float)Math.Sqrt(mag1) * (float)Math.Sqrt(mag2));
        }

        private static float[] ToFloatArray(byte[] bytes)
        {
            float[] floats = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }

        public static string GetDatabasePath()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingFileName);
                if (!File.Exists(path)) return string.Empty;
                byte[] encrypted = File.ReadAllBytes(path);
                byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                var settings = JsonSerializer.Deserialize<SystemSettingModel>(Encoding.UTF8.GetString(decrypted));
                return settings == null ? string.Empty : Path.Combine(settings.DbFolderPath, settings.DbFileName);
            }
            catch { return string.Empty; }
        }

        public static void DeleteEmployee(string code)
        {
            string dbPath = GetDatabasePath();
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                using (var cmd = new SqliteCommand("DELETE FROM Employees WHERE EmployeeCode=@c", conn))
                {
                    cmd.Parameters.AddWithValue("@c", code);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}