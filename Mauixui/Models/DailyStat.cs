using SQLite;
using System;

namespace Mauixui.Models
{
    [Table("DailyStat")]
    public class DailyStat
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Дата (храним без времени)
        public DateTime Date { get; set; }

        // Общее время в секундах
        public long TotalSeconds { get; set; }

        // Короткие поля для быстрого доступа
        public string TopApp { get; set; }
        public string TopSite { get; set; }

        // Подробные JSON-поля (apps/sites summaries)
        public string AppsJson { get; set; }
        public string SitesJson { get; set; }
    }
}
