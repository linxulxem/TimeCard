namespace NfcTimeCard.Models
{
    /// <summary>
    /// システム設定を保持する共通データモデル
    /// </summary>
    public class SystemSettingModel
    {
        // 管理者・データベース設定
        public string AdminId { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
        public string DbFolderPath { get; set; } = string.Empty;
        public string DbFileName { get; set; } = string.Empty;

        // 丸め処理設定
        public int InRoundingIndex { get; set; } = 1;
        public int OutRoundingIndex { get; set; } = 1;
        public int WorkRoundingIndex { get; set; } = 0;

        // 営業日の締め時間設定
        public int CutoffDayIndex { get; set; } = 0;   // 0: 本日, 1: 翌日
        public int CutoffHour { get; set; } = 0;       // 0～11
        public int CutoffMinute { get; set; } = 0;     // 0～59

        // ★追加：顔認証用ビデオデバイス設定
        /// <summary>
        /// 使用するカメラデバイスのインデックス (0, 1, 2...)
        /// </summary>
        public int VideoDeviceIndex { get; set; } = 0;
    }
}