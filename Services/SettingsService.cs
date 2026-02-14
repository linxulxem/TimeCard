using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace NfcTimeCard.Services;

public class AppSettings {
    public string AdminId { get; set; } = "linxulxem";
    public string AdminPw { get; set; } = "lxemmm.1177319";
    public bool IsFirstRun { get; set; } = true;
    public string DbPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
    public string DbFileName { get; set; } = "timecard.db";
    public string InRounding { get; set; } = "切り捨て";
    public string OutRounding { get; set; } = "切り上げ";
    public string RoundingUnit { get; set; } = "なし"; // なし, 15分, 30分
}

public static class SettingsService {
    private static readonly string FilePath = "config.dat";
    private static readonly byte[] Entropy = Encoding.ASCII.GetBytes("NfcTimeCardSecret");

    public static void Save(AppSettings settings) {
        string json = JsonConvert.SerializeObject(settings);
        byte[] data = Encoding.UTF8.GetBytes(json);
        byte[] encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, encrypted);
    }

    public static AppSettings Load() {
        if (!File.Exists(FilePath)) return new AppSettings();
        try {
            byte[] encrypted = File.ReadAllBytes(FilePath);
            byte[] data = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            return JsonConvert.DeserializeObject<AppSettings>(Encoding.UTF8.GetString(data)) ?? new AppSettings();
        } catch { return new AppSettings(); }
    }
}