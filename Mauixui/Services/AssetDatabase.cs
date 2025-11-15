using SQLite;
using Mauixui.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace Mauixui.Services
{
    public class AssetDatabase
    {
        private readonly SQLiteAsyncConnection _db;

        public AssetDatabase(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _db.CreateTableAsync<AssetItem>().Wait();
        }

        public Task<List<AssetItem>> GetAssetsAsync(string profileId)
        {
            return _db.Table<AssetItem>()
                .Where(a => a.ProfileId == profileId)
                .OrderByDescending(a => a.DateAcquired)
                .ToListAsync();
        }

        public Task<int> SaveAssetAsync(AssetItem item)
        {
            if (item.Id != 0) return _db.UpdateAsync(item);
            return _db.InsertAsync(item);
        }

        public Task<int> DeleteAssetAsync(AssetItem item)
        {
            return _db.DeleteAsync(item);
        }
    }
}
