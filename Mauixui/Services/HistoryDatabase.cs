using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mauixui.Models;
using System.Linq;

namespace Mauixui.Services
{
    public class HistoryDatabase
    {
        private readonly SQLiteAsyncConnection _db;

        public HistoryDatabase()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(folder, "activity_history.db3");
            _db = new SQLiteAsyncConnection(path);
            _db.CreateTableAsync<DailyStat>().Wait();
        }

        public Task<int> SaveStatAsync(DailyStat stat)
        {
            // Обновляем запись для даты (один stat на дату)
            var dt = stat.Date.Date;
            return (Task<int>)_db.RunInTransactionAsync(conn =>
            {
                var existing = conn.Find<DailyStat>(s => s.Date == dt);
                if (existing != null)
                {
                    // обновляем
                    existing.TotalSeconds = stat.TotalSeconds;
                    existing.TopApp = stat.TopApp;
                    existing.TopSite = stat.TopSite;
                    existing.AppsJson = stat.AppsJson;
                    existing.SitesJson = stat.SitesJson;
                    conn.Update(existing);
                }
                else
                {
                    stat.Date = dt;
                    conn.Insert(stat);
                }
            });
        }

        public async Task<List<DailyStat>> GetLastDaysAsync(int days)
        {
            var cutoff = DateTime.Today.AddDays(-days + 1);
            var all = await _db.Table<DailyStat>().Where(s => s.Date >= cutoff).OrderByDescending(s => s.Date).ToListAsync();

            // ensure we have exactly `days` elements — if missing, pad with empty entries for UI
            var result = new List<DailyStat>();
            for (int i = days - 1; i >= 0; i--)
            {
                var d = DateTime.Today.AddDays(-i).Date;
                var stat = all.FirstOrDefault(s => s.Date.Date == d);
                if (stat == null)
                {
                    stat = new DailyStat
                    {
                        Date = d,
                        TotalSeconds = 0,
                        TopApp = string.Empty,
                        TopSite = string.Empty,
                        AppsJson = "[]",
                        SitesJson = "[]"
                    };
                }
                result.Add(stat);
            }

            return result;
        }

        public Task<DailyStat> GetByDateAsync(DateTime date)
        {
            return _db.Table<DailyStat>().Where(s => s.Date == date.Date).FirstOrDefaultAsync();
        }
    }
}
