using System;

namespace NfcTimeCard.Services;

public static class TimeRoundingService {
    public static DateTime Round(DateTime dt, string mode, string unit) {
        int minutes = unit switch { "15分" => 15, "30分" => 30, _ => 0 };
        if (minutes == 0) return dt;

        long ticks = TimeSpan.FromMinutes(minutes).Ticks;
        if (mode == "切り捨て")
            return new DateTime(dt.Ticks / ticks * ticks);
        else // 切り上げ
            return new DateTime((dt.Ticks + ticks - 1) / ticks * ticks);
    }
}