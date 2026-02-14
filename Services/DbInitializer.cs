using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace NfcTimeCard.Services
{
    public static class DbInitializer
    {
        public static void CreateAndInitialize(string folderPath, string fileName)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string dbPath = Path.Combine(folderPath, fileName);
            string connectionString = $"Data Source={dbPath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 既存のテーブルを削除して再構築
                        string dropLogs = "DROP TABLE IF EXISTS AttendanceLogs;";
                        string dropEmployees = "DROP TABLE IF EXISTS Employees;";

                        // 従業員テーブル (顔写真と特徴量のBLOBカラムを追加)
                        string createEmployeesTable = @"
                            CREATE TABLE Employees (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                EmployeeCode TEXT NOT NULL UNIQUE,
                                Name TEXT NOT NULL,
                                Gender TEXT,
                                Address TEXT,
                                NfcId TEXT,
                                PhotoData BLOB,
                                FaceFeature BLOB
                            );";
                        
                        // 打刻ログテーブル
                        string createAttendanceLogsTable = @"
                            CREATE TABLE AttendanceLogs (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                EmployeeCode TEXT NOT NULL,
                                StampType TEXT NOT NULL,
                                ActualTime TEXT NOT NULL,
                                RoundedTime TEXT NOT NULL,
                                WorkDate TEXT NOT NULL,
                                CreatedAt TEXT DEFAULT (DATETIME('now', 'localtime')),
                                FOREIGN KEY (EmployeeCode) REFERENCES Employees(EmployeeCode)
                            );";

                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            
                            command.CommandText = dropLogs; command.ExecuteNonQuery();
                            command.CommandText = dropEmployees; command.ExecuteNonQuery();
                            command.CommandText = createEmployeesTable; command.ExecuteNonQuery();
                            command.CommandText = createAttendanceLogsTable; command.ExecuteNonQuery();
                            command.CommandText = "CREATE INDEX idx_logs_emp_date ON AttendanceLogs(EmployeeCode, WorkDate);";
                            command.CommandText = "CREATE INDEX idx_employees_nfc ON Employees(NfcId);";
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}