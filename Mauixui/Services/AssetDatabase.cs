using SQLite;
using Mauixui.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mauixui.Services
{
    public class AssetDatabase
    {
        private readonly SQLiteAsyncConnection _database;

        public AssetDatabase(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<Asset>().Wait();
        }

        public Task<List<Asset>> GetAssetsAsync(string profileId)
        {
            return _database.Table<Asset>()
                .Where(a => a.ProfileId == profileId)
                .ToListAsync();
        }

        public Task<int> SaveAssetAsync(Asset asset)
        {
            if (asset.Id != 0)
                return _database.UpdateAsync(asset);
            else
                return _database.InsertAsync(asset);
        }

        public Task<int> DeleteAssetAsync(Asset asset)
        {
            return _database.DeleteAsync(asset);
        }
    }
}
