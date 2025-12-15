using SQLite;
using Mauixui.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace Mauixui.Services
{
    public class TrackerDatabase : IDisposable
    {
        private SQLiteAsyncConnection _database;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly string _dbPath;

        // Конструктор принимает путь к БД
        public TrackerDatabase(string dbPath)
        {
            _dbPath = dbPath;
            _database = new SQLiteAsyncConnection(dbPath);
            Console.WriteLine($"📁 Создана БД трекера: {dbPath}");
        }

        /// <summary>
        /// Инициализация базы данных
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _lock.WaitAsync();
            try
            {
                if (_isInitialized) return;

                Console.WriteLine($"🔄 Создание таблиц в {Path.GetFileName(_dbPath)}...");

                await _database.CreateTableAsync<DailyStat>();
                await _database.CreateTableAsync<AppUsageRecord>();
                await _database.CreateTableAsync<WebsiteUsageRecord>();

                _isInitialized = true;
                Console.WriteLine($"✅ БД трекера инициализирована: {_dbPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка инициализации БД трекера: {ex.Message}");
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Получить базу данных трекера для текущего профиля
        /// </summary>
        public static async Task<TrackerDatabase> GetForCurrentProfileAsync()
        {
            var profileService = new ProfileService();
            var currentProfile = profileService.GetCurrentProfile();

            if (currentProfile == null)
            {
                throw new InvalidOperationException("Нет активного профиля");
            }

            var dbPath = profileService.GetTrackerDbPath(currentProfile.Id);
            var database = new TrackerDatabase(dbPath);
            await database.InitializeAsync();

            return database;
        }

        #region DailyStat Methods
        public async Task<DailyStat> GetTodayStatsAsync()
        {
            await EnsureInitializedAsync();

            var today = DateTime.Today;
            var todayString = today.ToString("yyyy-MM-dd");

            var stats = await _database.QueryAsync<DailyStat>(
                "SELECT * FROM DailyStat WHERE Date >= ? AND Date < ?",
                todayString, today.AddDays(1).ToString("yyyy-MM-dd"));

            var stat = stats.FirstOrDefault();

            if (stat == null)
            {
                stat = new DailyStat
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = today,
                    TotalSeconds = 0,
                    TopApp = "",
                    TopSite = "",
                    AppsJson = "[]",
                    SitesJson = "[]"
                };
                await _database.InsertAsync(stat);
                Console.WriteLine($"✅ Создана новая запись DailyStat за {todayString}");
            }

            return stat;
        }

        public async Task UpdateDailyStatAsync(DailyStat stat)
        {
            await EnsureInitializedAsync();
            await _database.UpdateAsync(stat);
            Console.WriteLine($"✅ DailyStat обновлен за {stat.Date:yyyy-MM-dd}");
        }

        public async Task<List<DailyStat>> GetLastDaysAsync(int days)
        {
            await EnsureInitializedAsync();

            var startDate = DateTime.Today.AddDays(-days + 1);
            return await _database.Table<DailyStat>()
                .Where(x => x.Date >= startDate)
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }

        public async Task<double> GetTotalTrackedTimeAsync()
        {
            await EnsureInitializedAsync();
            var stats = await _database.Table<DailyStat>().ToListAsync();
            return stats.Sum(x => x.TotalSeconds);
        }
        #endregion

        #region AppUsageRecord Methods
        public async Task SaveAppUsageAsync(AppUsageRecord record)
        {
            await EnsureInitializedAsync();

            // Убедимся, что Id установлен
            if (string.IsNullOrEmpty(record.Id))
            {
                record.Id = Guid.NewGuid().ToString();
            }

            await _database.InsertAsync(record);
            Console.WriteLine($"✅ Сохранена запись приложения: {record.AppName}");
        }

        public async Task<List<AppUsageRecord>> GetTodayAppUsageAsync()
        {
            await EnsureInitializedAsync();

            var today = DateTime.Today;
            var todayString = today.ToString("yyyy-MM-dd");

            return await _database.QueryAsync<AppUsageRecord>(
                "SELECT * FROM AppUsageRecord WHERE StartTime >= ? AND StartTime < ? ORDER BY StartTime DESC",
                todayString, today.AddDays(1).ToString("yyyy-MM-dd"));
        }

        public async Task<List<AppUsageRecord>> GetAppUsageByDateAsync(DateTime date)
        {
            await EnsureInitializedAsync();

            var dateString = date.ToString("yyyy-MM-dd");
            var nextDayString = date.AddDays(1).ToString("yyyy-MM-dd");

            return await _database.QueryAsync<AppUsageRecord>(
                "SELECT * FROM AppUsageRecord WHERE StartTime >= ? AND StartTime < ? ORDER BY StartTime DESC",
                dateString, nextDayString);
        }

        public async Task<List<AppUsageRecord>> GetAllAppUsageAsync()
        {
            await EnsureInitializedAsync();

            return await _database.Table<AppUsageRecord>()
                .OrderByDescending(x => x.StartTime)
                .ToListAsync();
        }
        #endregion

        #region WebsiteUsageRecord Methods
        public async Task SaveWebsiteUsageAsync(WebsiteUsageRecord record)
        {
            await EnsureInitializedAsync();

            // Добавляем ProfileId к записи
            var currentProfile = new ProfileService().GetCurrentProfile();
            if (currentProfile != null)
            {
                // record.ProfileId = currentProfile.Id;
            }

            await _database.InsertAsync(record);
            Console.WriteLine($"✅ Сохранена запись сайта: {record.Website}");
        }

        public async Task<List<WebsiteUsageRecord>> GetTodayWebsiteUsageAsync()
        {
            await EnsureInitializedAsync();

            var today = DateTime.Today;
            var todayString = today.ToString("yyyy-MM-dd");

            return await _database.QueryAsync<WebsiteUsageRecord>(
                "SELECT * FROM WebsiteUsageRecord WHERE StartTime >= ? AND StartTime < ? ORDER BY StartTime DESC",
                todayString, today.AddDays(1).ToString("yyyy-MM-dd"));
        }

        public async Task<List<WebsiteUsageRecord>> GetWebsiteUsageByDateAsync(DateTime date)
        {
            await EnsureInitializedAsync();

            var dateString = date.ToString("yyyy-MM-dd");
            var nextDayString = date.AddDays(1).ToString("yyyy-MM-dd");

            return await _database.QueryAsync<WebsiteUsageRecord>(
                "SELECT * FROM WebsiteUsageRecord WHERE StartTime >= ? AND StartTime < ? ORDER BY StartTime DESC",
                dateString, nextDayString);
        }

        public async Task<List<WebsiteUsageRecord>> GetAllWebsiteUsageAsync()
        {
            await EnsureInitializedAsync();

            return await _database.Table<WebsiteUsageRecord>()
                .OrderByDescending(x => x.StartTime)
                .ToListAsync();
        }
        #endregion

        #region Statistics Methods
        public async Task UpdateTodayStatFromRecordsAsync()
        {
            await EnsureInitializedAsync();

            try
            {
                var stat = await GetTodayStatsAsync();
                var todayApps = await GetTodayAppUsageAsync();
                var todayWebsites = await GetTodayWebsiteUsageAsync();

                // Общее время
                var totalSeconds = todayApps.Sum(a => (a.EndTime - a.StartTime).TotalSeconds) +
                                  todayWebsites.Sum(w => (w.EndTime - w.StartTime).TotalSeconds);

                stat.TotalSeconds = (long)totalSeconds;

                // Топ приложение
                var topApp = todayApps
                    .GroupBy(a => a.AppName)
                    .Select(g => new { App = g.Key, Seconds = g.Sum(a => (a.EndTime - a.StartTime).TotalSeconds) })
                    .OrderByDescending(x => x.Seconds)
                    .FirstOrDefault();

                stat.TopApp = topApp?.App ?? "Нет данных";

                // Топ сайт
                var topSite = todayWebsites
                    .GroupBy(w => w.Website)
                    .Select(g => new { Site = g.Key, Seconds = g.Sum(w => (w.EndTime - w.StartTime).TotalSeconds) })
                    .OrderByDescending(x => x.Seconds)
                    .FirstOrDefault();

                stat.TopSite = topSite?.Site ?? "Нет данных";

                // JSON статистика приложений
                var appSummaries = todayApps
                    .GroupBy(a => a.AppName)
                    .Select(g => new AppSummary
                    {
                        App = g.Key,
                        Seconds = (long)g.Sum(a => (a.EndTime - a.StartTime).TotalSeconds),
                        Category = g.FirstOrDefault()?.Category ?? "Другое"
                    })
                    .OrderByDescending(x => x.Seconds)
                    .ToList();

                stat.AppsJson = JsonSerializer.Serialize(appSummaries);

                // JSON статистика сайтов
                var siteSummaries = todayWebsites
                    .GroupBy(w => w.Website)
                    .Select(g => new SiteSummary
                    {
                        Site = g.Key,
                        Seconds = (long)g.Sum(w => (w.EndTime - w.StartTime).TotalSeconds),
                        Category = g.FirstOrDefault()?.Category ?? "Другое"
                    })
                    .OrderByDescending(x => x.Seconds)
                    .ToList();

                stat.SitesJson = JsonSerializer.Serialize(siteSummaries);

                await UpdateDailyStatAsync(stat);

                Console.WriteLine($"✅ Обновлена статистика за {DateTime.Today:yyyy-MM-dd}. " +
                    $"Общее время: {TimeSpan.FromSeconds(totalSeconds):hh\\:mm\\:ss}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления статистики: {ex.Message}");
            }
        }

        public async Task DeleteOldRecordsAsync(int daysToKeep = 30)
        {
            await EnsureInitializedAsync();

            try
            {
                var cutoffDate = DateTime.Today.AddDays(-daysToKeep);
                var cutoffString = cutoffDate.ToString("yyyy-MM-dd");

                // Удаляем старые записи
                await _database.ExecuteAsync(
                    "DELETE FROM AppUsageRecord WHERE StartTime < ?",
                    cutoffString);

                await _database.ExecuteAsync(
                    "DELETE FROM WebsiteUsageRecord WHERE StartTime < ?",
                    cutoffString);

                // Удаляем старые DailyStat
                await _database.ExecuteAsync(
                    "DELETE FROM DailyStat WHERE Date < ?",
                    cutoffDate);

                Console.WriteLine($"✅ Удалены записи старше {daysToKeep} дней");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка удаления старых записей: {ex.Message}");
            }
        }

        public async Task ClearAllDataAsync()
        {
            await EnsureInitializedAsync();

            try
            {
                await _database.DeleteAllAsync<AppUsageRecord>();
                await _database.DeleteAllAsync<WebsiteUsageRecord>();
                await _database.DeleteAllAsync<DailyStat>();

                Console.WriteLine("✅ Все данные трекера очищены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка очистки данных: {ex.Message}");
            }
        }

        public async Task<bool> CheckDatabaseConnection()
        {
            try
            {
                await EnsureInitializedAsync();

                var count = await _database.Table<AppUsageRecord>().CountAsync();
                Console.WriteLine($"✅ Подключение к БД успешно. Записей AppUsageRecord: {count}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения к БД: {ex.Message}");
                return false;
            }
        }
        #endregion

        private async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
        }

        public void Dispose()
        {
            _database?.CloseAsync().Wait();
        }
    }

    public class AppSummary
    {
        public string App { get; set; }
        public long Seconds { get; set; }
        public string Category { get; set; }
    }

    public class SiteSummary
    {
        public string Site { get; set; }
        public long Seconds { get; set; }
        public string Category { get; set; }
    }
}