using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using NfcTimeCard.Models;

namespace NfcTimeCard.Services
{
    public static class AttendanceService
    {
        private const string SettingFileName = "system_setting";

        // --- 打刻記録（メイン画面用） ---
        public static string? RecordStamp(Employee emp, string type, SystemSettingModel settings)
        {
            DateTime now = DateTime.Now;
            string dbPath = EmployeeService.GetDatabasePath();
            if (string.IsNullOrEmpty(dbPath)) throw new InvalidOperationException("DBパス未設定");

            string workDate = CalculateWorkDate(now, settings);
            if (GetStampCount(emp.EmployeeCode, workDate, dbPath) >= 10) throw new Exception("打刻回数制限です。");

            DateTime rounded = CalculateRoundedTime(now, type, settings);
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = "INSERT INTO AttendanceLogs (EmployeeCode, StampType, ActualTime, RoundedTime, WorkDate) VALUES (@c, @t, @a, @r, @w)";
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@c", emp.EmployeeCode);
                    cmd.Parameters.AddWithValue("@t", type);
                    cmd.Parameters.AddWithValue("@a", now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@r", rounded.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@w", workDate);
                    cmd.ExecuteNonQuery();
                }
            }
            return null;
        }

        // --- 打刻詳細データ取得（1ヶ月分カレンダー補完） ---
        public static List<DailyAttendance> GetDailyAttendanceList(string employeeCode, int year, int month)
        {
            string dbPath = EmployeeService.GetDatabasePath();
            var result = new List<DailyAttendance>();
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath)) return result;

            var allLogs = new List<AttendanceLog>();
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string filter = $"{year}-{month:D2}-%";
                using (var cmd = new SqliteCommand("SELECT * FROM AttendanceLogs WHERE EmployeeCode=@c AND WorkDate LIKE @f ORDER BY ActualTime ASC", conn))
                {
                    cmd.Parameters.AddWithValue("@c", employeeCode);
                    cmd.Parameters.AddWithValue("@f", filter);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read()) allLogs.Add(new AttendanceLog {
                            Id = r.GetInt32(0), EmployeeCode = r.GetString(1), StampType = r.GetString(2),
                            ActualTime = DateTime.Parse(r.GetString(3)), RoundedTime = DateTime.Parse(r.GetString(4)), WorkDate = r.GetString(5)
                        });
                    }
                }
            }

            var logGroups = allLogs.GroupBy(l => l.WorkDate).ToDictionary(g => g.Key, g => g.ToList());
            int daysInMonth = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= daysInMonth; day++)
            {
                string targetDate = $"{year}-{month:D2}-{day:D2}";
                result.Add(new DailyAttendance {
                    EmployeeCode = employeeCode,
                    WorkDate = targetDate,
                    Logs = logGroups.ContainsKey(targetDate) ? logGroups[targetDate] : new List<AttendanceLog>()
                });
            }
            return result;
        }

        // --- 打刻修正（一括削除＆再登録） ---
        public static void UpdateDailyLogs(string empCode, string workDate, List<(TextBox In, TextBox Out)> inputs)
        {
            string dbPath = EmployeeService.GetDatabasePath();
            var settings = GetSettingsInternal(); // ここでエラーが出ていた箇所を解決

            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        using (var del = new SqliteCommand("DELETE FROM AttendanceLogs WHERE EmployeeCode=@c AND WorkDate=@d", conn, trans))
                        {
                            del.Parameters.AddWithValue("@c", empCode);
                            del.Parameters.AddWithValue("@d", workDate);
                            del.ExecuteNonQuery();
                        }
                        foreach (var (inBox, outBox) in inputs)
                        {
                            if (!string.IsNullOrWhiteSpace(inBox.Text))
                                InsertManualLog(conn, trans, empCode, workDate, "IN", inBox.Text, settings);
                            if (!string.IsNullOrWhiteSpace(outBox.Text))
                                InsertManualLog(conn, trans, empCode, workDate, "OUT", outBox.Text, settings);
                        }
                        trans.Commit();
                    }
                    catch { trans.Rollback(); throw; }
                }
            }
        }

        private static void InsertManualLog(SqliteConnection conn, SqliteTransaction trans, string empCode, string workDate, string type, string timeStr, SystemSettingModel? settings)
        {
            if (string.IsNullOrWhiteSpace(timeStr) || timeStr == "-") return;
            if (!DateTime.TryParse($"{workDate} {timeStr}", out DateTime parsed)) throw new Exception($"{timeStr} の形式が不正です。");

            // 手動修正時も設定があれば「丸め時刻」を再計算
            DateTime rounded = (settings != null) ? CalculateRoundedTime(parsed, type, settings) : parsed;

            string sql = "INSERT INTO AttendanceLogs (EmployeeCode, StampType, ActualTime, RoundedTime, WorkDate) VALUES (@c, @t, @a, @r, @w)";
            using (var cmd = new SqliteCommand(sql, conn, trans))
            {
                cmd.Parameters.AddWithValue("@c", empCode);
                cmd.Parameters.AddWithValue("@t", type);
                cmd.Parameters.AddWithValue("@a", parsed.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@r", rounded.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@w", workDate);
                cmd.ExecuteNonQuery();
            }
        }

        // --- 設定ファイルの読み込み（内部用） ---
        private static SystemSettingModel? GetSettingsInternal()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingFileName);
                if (!File.Exists(path)) return null;
                byte[] encrypted = File.ReadAllBytes(path);
                byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return JsonSerializer.Deserialize<SystemSettingModel>(Encoding.UTF8.GetString(decrypted));
            }
            catch { return null; }
        }

        // --- 丸めロジック（切り上げ・切り捨て） ---
        public static DateTime CalculateRoundedTime(DateTime t, string type, SystemSettingModel settings)
        {
            int interval = settings.WorkRoundingIndex switch { 1 => 15, 2 => 30, _ => 1 };
            if (interval == 1) return t;

            int strategy = (type == "IN") ? settings.InRoundingIndex : settings.OutRoundingIndex;
            int minute = t.Minute;
            int newMinute = (strategy == 0) 
                ? ((minute + interval - 1) / interval) * interval // 切り上げ
                : (minute / interval) * interval;                // 切り捨て

            return new DateTime(t.Year, t.Month, t.Day, t.Hour, 0, 0).AddMinutes(newMinute);
        }

        public static string CalculateWorkDate(DateTime now, SystemSettingModel settings)
        {
            TimeSpan cutoff = new TimeSpan(settings.CutoffHour, settings.CutoffMinute, 0);
            return now.TimeOfDay < cutoff ? now.AddDays(-1).ToString("yyyy-MM-dd") : now.ToString("yyyy-MM-dd");
        }

        public static AttendanceLog? GetLatestStamp(string code)
        {
            string dbPath = EmployeeService.GetDatabasePath();
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath)) return null;
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = new SqliteCommand("SELECT * FROM AttendanceLogs WHERE EmployeeCode=@c ORDER BY ActualTime DESC LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@c", code);
            using var r = cmd.ExecuteReader();
            if (r.Read()) return new AttendanceLog {
                Id = r.GetInt32(0), EmployeeCode = r.GetString(1), StampType = r.GetString(2),
                ActualTime = DateTime.Parse(r.GetString(3)), RoundedTime = DateTime.Parse(r.GetString(4)), WorkDate = r.GetString(5)
            };
            return null;
        }

        public static int GetStampCount(string code, string workDate, string dbPath)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = new SqliteCommand("SELECT COUNT(*) FROM AttendanceLogs WHERE EmployeeCode=@c AND WorkDate=@w", conn);
            cmd.Parameters.AddWithValue("@c", code); cmd.Parameters.AddWithValue("@w", workDate);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}