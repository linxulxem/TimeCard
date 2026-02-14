using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using NfcTimeCard.Views;
using NfcTimeCard.Models; // 共通モデルを使用

namespace NfcTimeCard
{
    public partial class App : Application
    {
        private const string SettingFileName = "system_setting";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (IsSystemReady())
            {
                // すべての設定が整っていれば打刻画面へ
                new MainWindow().Show();
            }
            else
            {
                // 設定が不完全、またはDBが見つからなければ管理画面へ
                new AdminWindow().Show();
            }
        }

        private bool IsSystemReady()
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingFileName);
                if (!File.Exists(filePath)) return false;

                byte[] encryptedData = File.ReadAllBytes(filePath);
                byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                string jsonString = Encoding.UTF8.GetString(decryptedData);
                
                // 共通モデルへデシリアライズ
                var settings = JsonSerializer.Deserialize<SystemSettingModel>(jsonString);

                if (settings == null) return false;

                // 管理者情報・DB設定の未入力チェック
                if (string.IsNullOrWhiteSpace(settings.AdminId) || string.IsNullOrWhiteSpace(settings.AdminPassword)) return false;
                if (string.IsNullOrWhiteSpace(settings.DbFolderPath) || string.IsNullOrWhiteSpace(settings.DbFileName)) return false;

                // DBファイルの存在とフォーマットチェック
                string dbFullPath = Path.Combine(settings.DbFolderPath, settings.DbFileName);
                if (!File.Exists(dbFullPath) || !IsValidSqliteFile(dbFullPath)) return false;

                return true;
            }
            catch
            {
                // 復号エラーや読み取りエラーが発生した場合は未完了とみなす
                return false;
            }
        }

        /// <summary>
        /// ファイルが有効なSQLite形式かチェックする（CA2022対応版）
        /// </summary>
        private bool IsValidSqliteFile(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // SQLiteのヘッダーは16バイト以上必要
                    if (stream.Length < 16) return false;

                    byte[] header = new byte[16];

                    // ★修正箇所：CA2022 回避
                    // ReadExactly を使用することで、確実に16バイト読み取ることを保証します
                    stream.ReadExactly(header, 0, 16);

                    string headerString = Encoding.UTF8.GetString(header);

                    // SQLiteファイル特有のマジックナンバーを確認
                    return headerString.Contains("SQLite format 3");
                }
            }
            catch 
            { 
                return false; 
            }
        }
    }
}