using SQLite;
using Mauixui.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mauixui.Services
{
    public class FinanceDatabase
    {
        private readonly SQLiteAsyncConnection _db;

        public FinanceDatabase(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _db.CreateTableAsync<FinanceItem>().Wait();
        }

        // Метод без параметров
        public Task<List<FinanceItem>> GetItemsAsync()
        {
            return _db.Table<FinanceItem>().ToListAsync();
        }

        // Перегрузка с параметром profileId
        public Task<List<FinanceItem>> GetItemsAsync(string profileId)
        {
            return _db.Table<FinanceItem>()
                .Where(x => x.ProfileId == profileId)
                .ToListAsync();
        }

        // Метод с фильтрацией по profileId
        public Task<List<FinanceItem>> GetItemsByProfileAsync(string profileId)
        {
            return _db.Table<FinanceItem>()
                .Where(x => x.ProfileId == profileId)
                .ToListAsync();
        }

        public Task<int> SaveItemAsync(FinanceItem item)
        {
            if (item.Id != 0)
                return _db.UpdateAsync(item);
            else
                return _db.InsertAsync(item);
        }

        public Task<int> DeleteItemAsync(FinanceItem item)
        {
            return _db.DeleteAsync(item);
        }
    }
}
