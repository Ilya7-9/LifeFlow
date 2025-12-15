using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mauixui.Models;
using System.Linq.Expressions;

namespace Mauixui.Services
{
    public class MainDatabase : IDisposable
    {
        public const SQLiteOpenFlags Flags =
        // open the database in read/write mode
        SQLiteOpenFlags.ReadWrite |
        // create the database if it doesn't exist
        SQLiteOpenFlags.Create |
        // enable multi-threaded database access
        SQLiteOpenFlags.SharedCache;
        private static MainDatabase _instance;
        private SQLiteAsyncConnection _database;
        private readonly string _dbPath;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        // Singleton instance
        public static MainDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    var dbDir = @"D:\sqlite\db";

                    if (!Directory.Exists(dbDir))
                        Directory.CreateDirectory(dbDir);

                    var dbPath = Path.Combine(dbDir, "main_database.db3");

                    _instance = new MainDatabase(dbPath);
                }
                return _instance;
            }
        }

        private MainDatabase(string dbPath)
        {
            _dbPath = dbPath;
            Console.WriteLine($"📁 Создана главная БД: {dbPath}");
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                Console.WriteLine("🔄 Инициализация главной БД...");

                // Создаем директорию если не существует
                var directory = Path.GetDirectoryName(_dbPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"📁 Создана директория: {directory}");
                }

                // Создаем подключение
                _database = new SQLiteAsyncConnection(_dbPath, Flags);

                // ПРОСТО создаем таблицы без сложной логики
                Console.WriteLine("📊 Создание таблиц...");

                // Профили
                await _database.CreateTableAsync<UserProfile>();

                // Трекер
                await _database.CreateTableAsync<AppUsageRecord>();
                await _database.CreateTableAsync<WebsiteUsageRecord>();
                await _database.CreateTableAsync<DailyStat>();

                // Финансы
                await _database.CreateTableAsync<FinanceItem>();
                await _database.CreateTableAsync<CategoryItem>();
                await _database.CreateTableAsync<BudgetItem>();
                await _database.CreateTableAsync<AssetItem>();
                await _database.CreateTableAsync<DebtItem>();

                _isInitialized = true;
                Console.WriteLine($"✅ БД успешно инициализирована: {_dbPath}");

                // Проверяем файл
                if (File.Exists(_dbPath))
                {
                    var info = new FileInfo(_dbPath);
                    Console.WriteLine($"📊 Размер БД: {info.Length} байт");
                }
                else
                {
                    Console.WriteLine($"⚠️ Файл БД не создан: {_dbPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ КРИТИЧЕСКАЯ ОШИБКА инициализации БД: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw; // Пробрасываем дальше
            }
        }

        public string GetDatabasePath()
        {
            return _dbPath;
        }

        public async Task<long> GetTotalTrackedTimeAsync(string profileId)
        {
            await InitializeAsync();

            try
            {
                // Получаем все записи DailyStat для профиля
                var stats = await _database.Table<DailyStat>()
                    .Where(x => x.ProfileId == profileId)
                    .ToListAsync();

                // Суммируем вручную
                long totalSeconds = 0;
                foreach (var stat in stats)
                {
                    totalSeconds += stat.TotalSeconds;
                }

                return totalSeconds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения времени трекинга: {ex.Message}");
                return 0;
            }
        }

        private async Task CreateIndexesAsync()
        {
            // Исправляем создание индексов
            await CreateIndex<AppUsageRecord>(x => x.ProfileId);
            await CreateIndex<WebsiteUsageRecord>(x => x.ProfileId);
            await CreateIndex<DailyStat>(x => x.ProfileId);
            await CreateIndex<FinanceItem>(x => x.ProfileId);
            await CreateIndex<CategoryItem>(x => x.ProfileId);
            await CreateIndex<BudgetItem>(x => x.ProfileId);
            await CreateIndex<AssetItem>(x => x.ProfileId);
            await CreateIndex<DebtItem>(x => x.ProfileId);

            // Индексы по датам
            await CreateIndex<AppUsageRecord>(x => x.StartTime);
            await CreateIndex<WebsiteUsageRecord>(x => x.StartTime);
            await CreateIndex<DailyStat>(x => x.Date);
            await CreateIndex<FinanceItem>(x => x.Date);

            Console.WriteLine("✅ Индексы созданы");
        }

        private async Task CreateIndex<T>(Expression<Func<T, object>> property) where T : new()
        {
            try
            {
                await _database.CreateIndexAsync<T>(property, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка создания индекса для {typeof(T).Name}: {ex.Message}");
            }
        }

        public async Task<List<FinanceItem>> GetItemsAsync(string profileId)
        {
            await InitializeAsync();
            return await GetFinanceItemsAsync(profileId);
        }

        // Для совместимости со старым кодом:
        public async Task<List<FinanceItem>> GetItemsAsync()
        {
            await InitializeAsync();
            return await _database.Table<FinanceItem>().ToListAsync();
        }

        #region Profile Methods
        public async Task<UserProfile> CreateProfileAsync(string name, string email, string password, string avatar = "👤")
        {
            await InitializeAsync();

            var profile = new UserProfile
            {
                Name = name,
                Email = email.ToLower().Trim(),
                Password = password,
                Avatar = avatar,
                CreatedAt = DateTime.Now,
                LastLogin = DateTime.Now,
                IsActive = true
            };

            await _database.InsertAsync(profile);
            Console.WriteLine($"✅ Создан профиль: {profile.Name}");
            return profile;
        }

        public async Task<List<UserProfile>> GetProfilesAsync()
        {
            await InitializeAsync();
            return await _database.Table<UserProfile>().ToListAsync();
        }

        public async Task<UserProfile> GetProfileAsync(string profileId)
        {
            await InitializeAsync();
            return await _database.Table<UserProfile>()
                .Where(p => p.Id == profileId)
                .FirstOrDefaultAsync();
        }

        public async Task<UserProfile> GetProfileByEmailAsync(string email)
        {
            await InitializeAsync();
            return await _database.Table<UserProfile>()
                .Where(p => p.Email == email.ToLower().Trim())
                .FirstOrDefaultAsync();
        }

        public async Task UpdateProfileAsync(UserProfile profile)
        {
            await InitializeAsync();
            await _database.UpdateAsync(profile);
        }

        public async Task DeleteProfileAsync(string profileId)
        {
            await InitializeAsync();

            await _lock.WaitAsync();
            try
            {
                // Удаляем все данные профиля
                await _database.ExecuteAsync("DELETE FROM AppUsageRecord WHERE ProfileId = ?", profileId);
                await _database.ExecuteAsync("DELETE FROM WebsiteUsageRecord WHERE ProfileId = ?", profileId);
                await _database.ExecuteAsync("DELETE FROM DailyStat WHERE ProfileId = ?", profileId);
                await _database.ExecuteAsync("DELETE FROM FinanceItem WHERE ProfileId = ?", profileId);
                await _database.ExecuteAsync("DELETE FROM CategoryItem WHERE ProfileId = ?", profileId);
                await _database.ExecuteAsync("DELETE FROM BudgetItem WHERE ProfileId = ?", profileId);
                await _database.ExecuteAsync("DELETE FROM AssetItem WHERE ProfileId = ?", profileId);
                await _database.ExecuteAsync("DELETE FROM DebtItem WHERE ProfileId = ?", profileId);
                await _database.ExecuteAsync("DELETE FROM UserProfile WHERE Id = ?", profileId);

                Console.WriteLine($"✅ Профиль {profileId} и все его данные удалены");
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            await InitializeAsync();
            var count = await _database.Table<UserProfile>()
                .Where(p => p.Email == email.ToLower().Trim())
                .CountAsync();
            return count > 0;
        }

        // Метод для обновления времени трекинга в профиле (исправленная версия)
        public async Task UpdateProfileTrackedTimeAsync(string profileId)
        {
            await InitializeAsync();

            try
            {
                // Получаем все записи DailyStat для профиля
                var stats = await _database.Table<DailyStat>()
                    .Where(x => x.ProfileId == profileId)
                    .ToListAsync();

                // Суммируем вручную, так как SumAsync не поддерживается
                long totalSeconds = 0;
                foreach (var stat in stats)
                {
                    totalSeconds += stat.TotalSeconds;
                }

                // Обновляем профиль
                var profile = await GetProfileAsync(profileId);
                if (profile != null)
                {
                    profile.TotalTrackedSeconds = totalSeconds;
                    await UpdateProfileAsync(profile);
                    Console.WriteLine($"✅ Обновлено время трекинга для профиля {profile.Name}: {TimeSpan.FromSeconds(totalSeconds):hh\\:mm\\:ss}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления времени трекинга: {ex.Message}");
            }
        }
        #endregion

        #region Finance Methods
        public async Task<List<FinanceItem>> GetFinanceItemsAsync(string profileId)
        {
            await InitializeAsync();
            return await _database.Table<FinanceItem>()
                .Where(x => x.ProfileId == profileId)
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }

        public async Task<int> SaveFinanceItemAsync(FinanceItem item)
        {
            await InitializeAsync();
            if (item.Id != 0)
                return await _database.UpdateAsync(item);
            else
                return await _database.InsertAsync(item);
        }

        public async Task<int> DeleteFinanceItemAsync(FinanceItem item)
        {
            await InitializeAsync();
            return await _database.DeleteAsync(item);
        }
        #endregion

        #region Category Methods
        public async Task<List<CategoryItem>> GetCategoriesAsync(string profileId)
        {
            await InitializeAsync();
            return await _database.Table<CategoryItem>()
                .Where(x => x.ProfileId == profileId)
                .ToListAsync();
        }

        public async Task<int> SaveCategoryAsync(CategoryItem category)
        {
            await InitializeAsync();
            if (category.Id != 0)
                return await _database.UpdateAsync(category);
            else
                return await _database.InsertAsync(category);
        }

        public async Task<int> DeleteCategoryAsync(CategoryItem category)
        {
            await InitializeAsync();
            return await _database.DeleteAsync(category);
        }
        #endregion

        #region Budget Methods
        public async Task<List<BudgetItem>> GetBudgetsAsync(string profileId)
        {
            await InitializeAsync();
            return await _database.Table<BudgetItem>()
                .Where(x => x.ProfileId == profileId)
                .ToListAsync();
        }

        public async Task<int> SaveBudgetAsync(BudgetItem budget)
        {
            await InitializeAsync();
            if (budget.Id != 0)
                return await _database.UpdateAsync(budget);
            else
                return await _database.InsertAsync(budget);
        }

        public async Task<int> DeleteBudgetAsync(BudgetItem budget)
        {
            await InitializeAsync();
            return await _database.DeleteAsync(budget);
        }
        #endregion

        #region Asset Methods
        public async Task<List<AssetItem>> GetAssetsAsync(string profileId)
        {
            await InitializeAsync();
            return await _database.Table<AssetItem>()
                .Where(x => x.ProfileId == profileId)
                .OrderByDescending(x => x.DateAcquired)
                .ToListAsync();
        }

        public async Task<int> SaveAssetAsync(AssetItem item)
        {
            await InitializeAsync();
            if (item.Id != 0)
                return await _database.UpdateAsync(item);
            else
                return await _database.InsertAsync(item);
        }
        public async Task<int> DeleteAssetAsync(AssetItem item)
        {
            await InitializeAsync();
            return await _database.DeleteAsync(item);
        }

        // Для совместимости
        public async Task<List<AssetItem>> GetItemsByProfileAsync(string profileId)
        {
            return await GetAssetsAsync(profileId);
        }
        #endregion

        #region Debt Methods
        public async Task<List<DebtItem>> GetDebtsAsync(string profileId)
        {
            await InitializeAsync();
            return await _database.Table<DebtItem>()
                .Where(x => x.ProfileId == profileId)
                .OrderBy(x => x.DueDate)
                .ToListAsync();
        }

        public async Task<int> SaveDebtAsync(DebtItem item)
        {
            await InitializeAsync();
            if (item.Id != 0)
                return await _database.UpdateAsync(item);
            else
                return await _database.InsertAsync(item);
        }

        public async Task<int> DeleteDebtAsync(DebtItem item)
        {
            await InitializeAsync();
            return await _database.DeleteAsync(item);
        }
        #endregion

        #region Tracker Methods
        public async Task SaveAppUsageAsync(AppUsageRecord record)
        {
            await InitializeAsync();
            if (string.IsNullOrEmpty(record.Id))
                record.Id = Guid.NewGuid().ToString();

            await _database.InsertAsync(record);
        }

        public async Task SaveWebsiteUsageAsync(WebsiteUsageRecord record)
        {
            await InitializeAsync();
            if (string.IsNullOrEmpty(record.Id))
                record.Id = Guid.NewGuid().ToString();

            await _database.InsertAsync(record);
        }

        public async Task<DailyStat> GetTodayStatsAsync(string profileId)
        {
            await InitializeAsync();

            var today = DateTime.Today;
            var stat = await _database.Table<DailyStat>()
                .Where(x => x.ProfileId == profileId && x.Date == today)
                .FirstOrDefaultAsync();

            if (stat == null)
            {
                stat = new DailyStat
                {
                    Id = Guid.NewGuid().ToString(),
                    ProfileId = profileId,
                    Date = today,
                    TotalSeconds = 0,
                    TopApp = "",
                    TopSite = "",
                    AppsJson = "[]",
                    SitesJson = "[]"
                };
                await _database.InsertAsync(stat);
            }

            return stat;
        }

        public async Task UpdateDailyStatAsync(DailyStat stat)
        {
            await InitializeAsync();
            await _database.UpdateAsync(stat);
        }

        public async Task<List<AppUsageRecord>> GetTodayAppUsageAsync(string profileId)
        {
            await InitializeAsync();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _database.Table<AppUsageRecord>()
                .Where(x => x.ProfileId == profileId && x.StartTime >= today && x.StartTime < tomorrow)
                .OrderByDescending(x => x.StartTime)
                .ToListAsync();
        }

        public async Task<List<WebsiteUsageRecord>> GetTodayWebsiteUsageAsync(string profileId)
        {
            await InitializeAsync();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _database.Table<WebsiteUsageRecord>()
                .Where(x => x.ProfileId == profileId && x.StartTime >= today && x.StartTime < tomorrow)
                .OrderByDescending(x => x.StartTime)
                .ToListAsync();
        }

        public async Task<List<DailyStat>> GetLastDaysAsync(string profileId, int days)
        {
            await InitializeAsync();

            var startDate = DateTime.Today.AddDays(-days + 1);
            return await _database.Table<DailyStat>()
                .Where(x => x.ProfileId == profileId && x.Date >= startDate)
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }
        #endregion

        #region Statistics Methods
        public async Task<(decimal totalIncome, decimal totalExpense)> GetFinanceStatsAsync(string profileId, DateTime? startDate = null, DateTime? endDate = null)
        {
            await InitializeAsync();

            var query = _database.Table<FinanceItem>()
                .Where(x => x.ProfileId == profileId);

            if (startDate.HasValue)
                query = query.Where(x => x.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Date <= endDate.Value);

            var items = await query.ToListAsync();

            // Считаем вручную
            decimal totalIncome = 0;
            decimal totalExpense = 0;

            foreach (var item in items)
            {
                if (item.Type == "Доход")
                    totalIncome += item.Amount;
                else if (item.Type == "Расход")
                    totalExpense += item.Amount;
            }

            return (totalIncome, totalExpense);
        }

        public async Task<Dictionary<string, decimal>> GetCategoryStatsAsync(string profileId, DateTime? startDate = null, DateTime? endDate = null)
        {
            await InitializeAsync();

            var query = _database.Table<FinanceItem>()
                .Where(x => x.ProfileId == profileId && x.Type == "Расход");

            if (startDate.HasValue)
                query = query.Where(x => x.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Date <= endDate.Value);

            var items = await query.ToListAsync();

            var result = new Dictionary<string, decimal>();
            foreach (var item in items)
            {
                if (result.ContainsKey(item.Category))
                    result[item.Category] += item.Amount;
                else
                    result[item.Category] = item.Amount;
            }

            return result;
        }
        #endregion

        public void Dispose()
        {
            _database?.CloseAsync().Wait();
        }
    }
}