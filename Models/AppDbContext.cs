using Microsoft.EntityFrameworkCore;
using NfcTimeCard.Models;

namespace NfcTimeCard.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<Employee> Employees { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // ここでは簡易的にパスを指定していますが、実際は設定ファイルから取得します
            optionsBuilder.UseSqlite("Data Source=TimeCard.db");
        }
    }
}