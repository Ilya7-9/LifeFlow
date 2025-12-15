using Mauixui.Models;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Mauixui.Services
{
    public partial class ProfileService
    {
        private readonly MainDatabase _database;
        private UserProfile _currentProfile;
        private List<UserProfile> _profilesCache;
        private readonly string _profilesFilePath;

        public ProfileService(string customPath = null)
        {
            _database = MainDatabase.Instance;

            if (!string.IsNullOrEmpty(customPath))
            {
                _profilesFilePath = Path.Combine(customPath, "user_profiles.json");
            }
            else
            {
                var dbDir = "D:/Шарага/С#/db";
                if (!Directory.Exists(dbDir))
                    Directory.CreateDirectory(dbDir);

                _profilesFilePath = Path.Combine(dbDir, "user_profiles.json");
            }

            _profilesCache = new List<UserProfile>();
            Console.WriteLine($"📁 Файл профилей: {_profilesFilePath}");
        }

        public async Task InitializeAsync()
        {
            await LoadProfilesFromDatabaseAsync();
        }

        private async Task LoadProfilesFromDatabaseAsync()
        {
            try
            {
                await _database.InitializeAsync();
                _profilesCache = await _database.GetProfilesAsync();
                Console.WriteLine($"✅ Загружено {_profilesCache.Count} профилей из БД");

                // Загружаем текущий профиль
                var currentProfileId = Preferences.Get("current_profile_id", "");
                if (!string.IsNullOrEmpty(currentProfileId))
                {
                    _currentProfile = _profilesCache.FirstOrDefault(p => p.Id == currentProfileId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки профилей из БД: {ex.Message}");
                _profilesCache = new List<UserProfile>();
            }
        }

        public List<UserProfile> GetProfiles()
        {
            try
            {
                if (_profilesCache.Any())
                    return _profilesCache;

                // Если нет в кэше, загружаем из БД
                var task = _database.GetProfilesAsync();
                task.Wait();
                _profilesCache = task.Result;

                return _profilesCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки профилей: {ex.Message}");
                return new List<UserProfile>();
            }
        }

        public async Task<List<UserProfile>> GetProfilesAsync()
        {
            try
            {
                if (_profilesCache.Any())
                    return _profilesCache;

                _profilesCache = await _database.GetProfilesAsync();
                return _profilesCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки профилей: {ex.Message}");
                return new List<UserProfile>();
            }
        }

        private UserProfile CreateDefaultProfile()
        {
            return new UserProfile
            {
                Name = "Основной профиль",
                Email = "default@user.com",
                Password = "",
                Avatar = "👤",
                CreatedAt = DateTime.Now,
                Theme = "Unspecified",
                AccentColor = "#5865F2",
                TotalTrackedSeconds = 0
            };
        }

        public void AddProfile(UserProfile profile)
        {
            try
            {
                // Добавляем в БД
                var task = _database.CreateProfileAsync(
                    profile.Name,
                    profile.Email ?? $"{profile.Id}@user.com",
                    profile.Password ?? "",
                    profile.Avatar);
                task.Wait();

                var newProfile = task.Result;
                _profilesCache.Add(newProfile);

                Console.WriteLine($"✅ Профиль добавлен: {profile.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления профиля: {ex.Message}");
            }
        }

        public async Task AddProfileAsync(UserProfile profile)
        {
            try
            {
                var newProfile = await _database.CreateProfileAsync(
                    profile.Name,
                    profile.Email ?? $"{profile.Id}@user.com",
                    profile.Password ?? "",
                    profile.Avatar);

                _profilesCache.Add(newProfile);
                Console.WriteLine($"✅ Профиль добавлен: {profile.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления профиля: {ex.Message}");
            }
        }

        public void UpdateProfile(UserProfile profile)
        {
            try
            {
                // Обновляем в БД
                var task = _database.UpdateProfileAsync(profile);
                task.Wait();

                // Обновляем в кэше
                var existing = _profilesCache.FirstOrDefault(p => p.Id == profile.Id);
                if (existing != null)
                {
                    _profilesCache.Remove(existing);
                    _profilesCache.Add(profile);
                }

                Console.WriteLine($"✅ Профиль обновлен: {profile.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления профиля: {ex.Message}");
            }
        }

        public async Task UpdateProfileAsync(UserProfile profile)
        {
            try
            {
                await _database.UpdateProfileAsync(profile);

                // Обновляем в кэше
                var existing = _profilesCache.FirstOrDefault(p => p.Id == profile.Id);
                if (existing != null)
                {
                    _profilesCache.Remove(existing);
                    _profilesCache.Add(profile);
                }

                Console.WriteLine($"✅ Профиль обновлен: {profile.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления профиля: {ex.Message}");
            }
        }

        public void DeleteProfile(string profileId)
        {
            try
            {
                // Удаляем из БД
                var task = _database.DeleteProfileAsync(profileId);
                task.Wait();

                // Удаляем из кэша
                var profile = _profilesCache.FirstOrDefault(p => p.Id == profileId);
                if (profile != null)
                {
                    _profilesCache.Remove(profile);

                    if (_currentProfile?.Id == profileId)
                    {
                        _currentProfile = null;
                        Preferences.Remove("current_profile_id");
                    }

                    Console.WriteLine($"✅ Профиль удален: {profile.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления профиля: {ex.Message}");
            }
        }

        public async Task DeleteProfileAsync(string profileId)
        {
            try
            {
                await _database.DeleteProfileAsync(profileId);

                // Удаляем из кэша
                var profile = _profilesCache.FirstOrDefault(p => p.Id == profileId);
                if (profile != null)
                {
                    _profilesCache.Remove(profile);

                    if (_currentProfile?.Id == profileId)
                    {
                        _currentProfile = null;
                        Preferences.Remove("current_profile_id");
                    }

                    Console.WriteLine($"✅ Профиль удален: {profile.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления профиля: {ex.Message}");
            }
        }

        public UserProfile GetCurrentProfile()
        {
            if (_currentProfile == null)
            {
                var profileId = Preferences.Get("current_profile_id", "");
                if (!string.IsNullOrEmpty(profileId))
                {
                    _currentProfile = _profilesCache.FirstOrDefault(p => p.Id == profileId);
                }

                if (_currentProfile == null && _profilesCache.Any())
                {
                    _currentProfile = _profilesCache.First();
                    SetCurrentProfile(_currentProfile);
                }
            }

            return _currentProfile;
        }

        public async Task<UserProfile> GetCurrentProfileAsync()
        {
            if (_currentProfile == null)
            {
                var profileId = Preferences.Get("current_profile_id", "");
                if (!string.IsNullOrEmpty(profileId))
                {
                    _currentProfile = await _database.GetProfileAsync(profileId);
                }

                if (_currentProfile == null)
                {
                    var profiles = await GetProfilesAsync();
                    if (profiles.Any())
                    {
                        _currentProfile = profiles.First();
                        SetCurrentProfile(_currentProfile);
                    }
                }
            }

            return _currentProfile;
        }

        public void SetCurrentProfile(UserProfile profile)
        {
            _currentProfile = profile;
            Preferences.Set("current_profile_id", profile.Id);
            Console.WriteLine($"✅ Установлен текущий профиль: {profile.Name}");
        }

        public UserProfile CreateProfile(string name, string avatar)
        {
            var profile = new UserProfile
            {
                Name = name,
                Email = $"{Guid.NewGuid().ToString()[..8]}@user.com",
                Password = "",
                Avatar = avatar,
                CreatedAt = DateTime.Now,
                Theme = "Unspecified",
                AccentColor = "#5865F2",
                TotalTrackedSeconds = 0
            };

            AddProfile(profile);
            Console.WriteLine($"✅ Создан новый профиль: {name}");
            return profile;
        }

        public async Task<UserProfile> CreateProfileAsync(string name, string avatar)
        {
            var profile = new UserProfile
            {
                Name = name,
                Email = $"{Guid.NewGuid().ToString()[..8]}@user.com",
                Password = "",
                Avatar = avatar,
                CreatedAt = DateTime.Now,
                Theme = "Unspecified",
                AccentColor = "#5865F2",
                TotalTrackedSeconds = 0
            };

            await AddProfileAsync(profile);
            Console.WriteLine($"✅ Создан новый профиль: {name}");
            return profile;
        }

        public string GetProfilesFilePath()
        {
            return _profilesFilePath;
        }

        // Методы для экспорта/импорта (теперь не используются, данные в БД)
        public async Task ExportToLocationAsync(string exportPath)
        {
            try
            {
                var profiles = await GetProfilesAsync();
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(profiles, options);

                var directory = Path.GetDirectoryName(exportPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(exportPath, json);
                Console.WriteLine($"📤 Экспортировано в: {exportPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка экспорта: {ex.Message}");
                throw;
            }
        }

        public async Task ImportFromFileAsync(string importFilePath)
        {
            try
            {
                if (!File.Exists(importFilePath))
                    throw new FileNotFoundException("Файл не найден", importFilePath);

                var json = await File.ReadAllTextAsync(importFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var importedProfiles = JsonSerializer.Deserialize<List<UserProfile>>(json, options);

                if (importedProfiles != null)
                {
                    foreach (var profile in importedProfiles)
                    {
                        await AddProfileAsync(profile);
                    }

                    Console.WriteLine($"📥 Импортировано {importedProfiles.Count} профилей");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка импорта: {ex.Message}");
                throw;
            }
        }

        public void OpenFileLocation()
        {
            try
            {
                var filePath = GetProfilesFilePath();
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{filePath}\"");
                }
                else
                {
                    var directory = Path.GetDirectoryName(filePath);
                    System.Diagnostics.Process.Start("explorer.exe", directory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Не удалось открыть расположение файла: {ex.Message}");
            }
        }

        // Метод для трекера времени (теперь не нужен отдельный файл)
        public string GetTrackerDbPath(string profileId)
        {
            // Возвращаем путь к единой БД
            return MainDatabase.Instance.GetDatabasePath();
        }

        // Методы для работы с трекером (через MainDatabase)
        public async Task<TrackerDatabase> GetTrackerDatabaseAsync(string profileId = null)
        {
            // Теперь используем единую БД через MainDatabase
            return null; // TrackerDatabase больше не используется отдельно
        }

        // ФИНАНСОВЫЕ МЕТОДЫ (теперь через MainDatabase)
        public MainDatabase GetFinanceDatabase(string profileId = null)
        {
            return MainDatabase.Instance;
        }

        public MainDatabase GetCategoryDatabase(string profileId = null)
        {
            return MainDatabase.Instance;
        }

        public MainDatabase GetBudgetDatabase(string profileId = null)
        {
            return MainDatabase.Instance;
        }

        public MainDatabase GetAssetDatabase(string profileId = null)
        {
            return MainDatabase.Instance;
        }

        public MainDatabase GetDebtDatabase(string profileId = null)
        {
            return MainDatabase.Instance;
        }

        // Обновление статистики профиля из трекера
        public async Task UpdateProfileStatsAsync(UserProfile profile)
        {
            try
            {
                if (profile != null)
                {
                    // Получаем общее время трекинга из БД
                    var totalSeconds = await _database.GetTotalTrackedTimeAsync(profile.Id);

                    profile.TotalTrackedSeconds = totalSeconds;
                    await UpdateProfileAsync(profile);

                    Console.WriteLine($"✅ Обновлена статистика профиля {profile.Name}: {TimeSpan.FromSeconds(totalSeconds):hh\\:mm\\:ss}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка обновления статистики: {ex.Message}");
            }
        }

        public async Task UpdateAllProfilesStatsAsync()
        {
            try
            {
                var profiles = await GetProfilesAsync();
                foreach (var profile in profiles)
                {
                    await UpdateProfileStatsAsync(profile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления статистики профилей: {ex.Message}");
            }
        }

        public void UpdateTrackerTime(TimeSpan trackedTime)
        {
            var profile = GetCurrentProfile();
            if (profile != null)
            {
                profile.TotalTrackedSeconds = (long)trackedTime.TotalSeconds;
                UpdateProfile(profile);
                Console.WriteLine($"✅ Обновлено время трекера: {trackedTime}");
            }
        }

        public async Task UpdateProfileStatsFromTrackerAsync()
        {
            var profile = GetCurrentProfile();
            if (profile != null)
            {
                await UpdateProfileStatsAsync(profile);
            }
        }
    }
}