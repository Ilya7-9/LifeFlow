using SQLite;
using Mauixui.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mauixui.Services
{
    public class CategoryDatabase
    {
        private readonly SQLiteAsyncConnection _database;

        public CategoryDatabase(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<CategoryItem>().Wait();
        }

        public Task<List<CategoryItem>> GetCategoriesAsync(string profileId)
        {
            return _database.Table<CategoryItem>()
                .Where(c => c.ProfileId == profileId)
                .ToListAsync();
        }

        public Task<int> SaveCategoryAsync(CategoryItem category)
        {
            if (category.Id != 0)
                return _database.UpdateAsync(category);
            else
                return _database.InsertAsync(category);
        }

        public Task<int> DeleteCategoryAsync(CategoryItem category)
        {
            return _database.DeleteAsync(category);
        }
    }
}