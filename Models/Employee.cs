namespace NfcTimeCard.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string NfcId { get; set; } = string.Empty;

        // 顔写真バイナリ (SQLiteのBLOB型に対応)
        public byte[]? PhotoData { get; set; }

        // 顔識別用の特徴量データ (数値配列のバイナリ)
        public byte[]? FaceFeature { get; set; }
    }
}