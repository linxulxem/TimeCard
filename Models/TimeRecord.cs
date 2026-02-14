using System;

namespace NfcTimeCard.Models;

public class TimeRecord {
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "出勤"; // 出勤 or 退勤
}