using SQLite;
using Mauixui.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mauixui.Services
{
    public class TrackerDatabase
    {
        private SQLiteAsyncConnection _database;

        public TrackerDatabase(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<DailyStat>().Wait();
        }

        public Task<List<DailyStat>> GetItemsAsync()
        {
            return _database.Table<DailyStat>().ToListAsync();
        }

        public Task<DailyStat> GetTodayStatsAsync()
        {
            var today = DateTime.Today;
            return _database.Table<DailyStat>()
                .Where(x => x.Date == today)
                .FirstOrDefaultAsync();
        }

        public Task<List<DailyStat>> GetWeekStatsAsync()
        {
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);
            return _database.Table<DailyStat>()
                .Where(x => x.Date >= startOfWeek && x.Date < endOfWeek)
                .ToListAsync();
        }
    }
}