namespace NfcTimeCard.Models
{
    public class AttendanceLog
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        
        // 打刻種別: "IN" (出勤) / "OUT" (退勤)
        public string StampType { get; set; } = string.Empty;

        // 実際の打刻日時（記録用）
        public DateTime ActualTime { get; set; }

        // 設定に基づき丸め処理された日時（計算用）
        public DateTime RoundedTime { get; set; }

        // ★重要：運用上の日付（例：2月13日 深夜1時の打刻でも、設定により「2月12日分」として保存）
        // 型を string (yyyy-MM-dd) にしておくとSQLiteでのGROUP BYが非常に楽になります。
        public string WorkDate { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}