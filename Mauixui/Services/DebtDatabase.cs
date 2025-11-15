using SQLite;
using Mauixui.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mauixui.Services
{
    public class DebtDatabase
    {
        private readonly SQLiteAsyncConnection _db;

        public DebtDatabase(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _db.CreateTableAsync<DebtItem>().Wait();
        }

        public Task<List<DebtItem>> GetDebtsAsync(string profileId)
        {
            return _db.Table<DebtItem>()
                .Where(d => d.ProfileId == profileId)
                .OrderBy(d => d.DueDate)
                .ToListAsync();
        }

        public Task<int> SaveDebtAsync(DebtItem item)
        {
            if (item.Id != 0) return _db.UpdateAsync(item);
            return _db.InsertAsync(item);
        }

        public Task<int> DeleteDebtAsync(DebtItem item)
        {
            return _db.DeleteAsync(item);
        }
    }
}
