using SQLite;
using Mauixui.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mauixui.Services
{
    public class BudgetDatabase
    {
        private readonly SQLiteAsyncConnection _database;

        public BudgetDatabase(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<Budget>().Wait();
        }

        public Task<List<Budget>> GetBudgetsAsync(string profileId)
        {
            return _database.Table<Budget>()
                .Where(b => b.ProfileId == profileId)
                .ToListAsync();
        }

        public Task<int> SaveBudgetAsync(Budget budget)
        {
            if (budget.Id != 0)
                return _database.UpdateAsync(budget);
            else
                return _database.InsertAsync(budget);
        }

        public Task<int> DeleteBudgetAsync(Budget budget)
        {
            return _database.DeleteAsync(budget);
        }
    }
}
