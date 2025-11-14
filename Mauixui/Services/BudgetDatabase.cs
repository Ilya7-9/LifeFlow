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
            _database.CreateTableAsync<BudgetItem>().Wait();
        }

        public Task<List<BudgetItem>> GetBudgetsAsync(string profileId)
        {
            return _database.Table<BudgetItem>()
                .Where(b => b.ProfileId == profileId)
                .ToListAsync();
        }

        public Task<int> SaveBudgetAsync(BudgetItem budget)
        {
            if (budget.Id != 0)
                return _database.UpdateAsync(budget);
            else
                return _database.InsertAsync(budget);
        }

        public Task<int> DeleteBudgetAsync(BudgetItem budget)
        {
            return _database.DeleteAsync(budget);
        }
    }
}
