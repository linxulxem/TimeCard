using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NfcTimeCard.Models
{
    public class DailyAttendance
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string WorkDate { get; set; } = string.Empty;
        public List<AttendanceLog> Logs { get; set; } = new List<AttendanceLog>();

        public string DisplayDate
        {
            get
            {
                if (DateTime.TryParse(WorkDate, out DateTime dt))
                    return dt.ToString("MM/dd (ddd)", new CultureInfo("ja-JP"));
                return WorkDate;
            }
        }

        public int DayOfWeek => DateTime.TryParse(WorkDate, out DateTime dt) ? (int)dt.DayOfWeek : -1;

        public string TotalWorkTime
        {
            get
            {
                TimeSpan total = TimeSpan.Zero;
                var sorted = Logs.OrderBy(l => l.ActualTime).ToList();
                for (int i = 0; i < sorted.Count - 1; i++)
                {
                    if (sorted[i].StampType == "IN" && sorted[i + 1].StampType == "OUT")
                    {
                        TimeSpan diff = sorted[i + 1].RoundedTime - sorted[i].RoundedTime;
                        if (diff > TimeSpan.Zero) total += diff;
                        i++;
                    }
                }
                return total == TimeSpan.Zero ? "-" : total.TotalHours.ToString("0.##");
            }
        }

        public string In1A => GetLog(0, "IN", false);
        public string In1R => GetLog(0, "IN", true);
        public string Out1A => GetLog(0, "OUT", false);
        public string Out1R => GetLog(0, "OUT", true);
        public string In2A => GetLog(1, "IN", false);
        public string In2R => GetLog(1, "IN", true);
        public string Out2A => GetLog(1, "OUT", false);
        public string Out2R => GetLog(1, "OUT", true);
        public string In3A => GetLog(2, "IN", false);
        public string In3R => GetLog(2, "IN", true);
        public string Out3A => GetLog(2, "OUT", false);
        public string Out3R => GetLog(2, "OUT", true);
        public string In4A => GetLog(3, "IN", false);
        public string In4R => GetLog(3, "IN", true);
        public string Out4A => GetLog(3, "OUT", false);
        public string Out4R => GetLog(3, "OUT", true);
        public string In5A => GetLog(4, "IN", false);
        public string In5R => GetLog(4, "IN", true);
        public string Out5A => GetLog(4, "OUT", false);
        public string Out5R => GetLog(4, "OUT", true);

        private string GetLog(int index, string type, bool isRounded)
        {
            var targets = Logs.Where(l => l.StampType == type).OrderBy(l => l.ActualTime).ToList();
            if (targets.Count <= index) return "-";
            return isRounded ? targets[index].RoundedTime.ToString("HH:mm") : targets[index].ActualTime.ToString("HH:mm");
        }
    }
}