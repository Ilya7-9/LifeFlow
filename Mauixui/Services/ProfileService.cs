using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mauixui.Models;
using Microsoft.Maui.Storage;

namespace Mauixui.Services
{
    public partial class ProfileService
    {
        // ДОБАВИМ МЕТОД ДЛЯ ТРЕКЕРА
        public TrackerDatabase GetTrackerDatabase(string profileId)
        {
            var path = Path.Combine("D:/Шарага/С#/db", $"{profileId}_tracker.db3");
            return new TrackerDatabase(path);
        }

        // Остальные методы остаются...
        public void AddProfile(UserProfile profile)
        {
            try
            {
                var profiles = GetProfiles();
                profiles.Add(profile);
                SaveProfiles(profiles);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления профиля: {ex.Message}");
            }
        }

        private string _currentProfileId;

        public ProfileService()
        {
            _currentProfileId = Preferences.Get("current_profile_id", "");
        }

        public async Task UpdateAllProfilesStatsAsync()
        {
            try
            {
                var profiles = GetProfiles();
                foreach (var profile in profiles)
                {
                    await UpdateProfileStatistics(profile);
                }
                SaveProfiles(profiles);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления статистики профилей: {ex.Message}");
            }
        }

        public async Task UpdateProfileStatistics(UserProfile profile)
        {
            try
            {
                var trackedTime = TimeSpan.Zero;
                profile.TotalTrackedTime = trackedTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления статистики для профиля {profile.Name}: {ex.Message}");
            }
        }

        private void SaveProfiles(List<UserProfile> profiles)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(profiles);
                Preferences.Set("user_profiles", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения профилей: {ex.Message}");
            }
        }

        public List<UserProfile> GetProfiles()
        {
            try
            {
                var json = Preferences.Get("user_profiles", "[]");
                var profiles = System.Text.Json.JsonSerializer.Deserialize<List<UserProfile>>(json) ?? new List<UserProfile>();

                if (!profiles.Any())
                {
                    var defaultProfile = new UserProfile
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Основной профиль",
                        Avatar = "👤",
                        CreatedAt = DateTime.Now,
                        Theme = AppTheme.Unspecified,
                        AccentColor = "#5865F2"
                    };
                    profiles.Add(defaultProfile);
                    SaveProfiles(profiles);
                    SetCurrentProfile(defaultProfile);
                }

                return profiles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки профилей: {ex.Message}");
                return new List<UserProfile>();
            }
        }

        public UserProfile GetCurrentProfile()
        {
            var profiles = GetProfiles();
            return profiles.FirstOrDefault(p => p.Id == _currentProfileId) ?? profiles.FirstOrDefault();
        }

        public void SetCurrentProfile(UserProfile profile)
        {
            _currentProfileId = profile.Id;
            Preferences.Set("current_profile_id", profile.Id);
        }

        public UserProfile CreateProfile(string name, string avatar)
        {
            var profile = new UserProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Avatar = avatar,
                CreatedAt = DateTime.Now,
                Theme = AppTheme.Unspecified,
                AccentColor = "#5865F2"
            };

            var profiles = GetProfiles();
            profiles.Add(profile);
            SaveProfiles(profiles);

            return profile;
        }

        public void UpdateProfile(UserProfile profile)
        {
            var profiles = GetProfiles();
            var existing = profiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existing != null)
            {
                profiles.Remove(existing);
                profiles.Add(profile);
                SaveProfiles(profiles);
            }
        }

        public void DeleteProfile(string profileId)
        {
            var profiles = GetProfiles();
            var profile = profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile != null)
            {
                profiles.Remove(profile);
                SaveProfiles(profiles);

                if (_currentProfileId == profileId)
                {
                    _currentProfileId = profiles.FirstOrDefault()?.Id ?? "";
                    Preferences.Set("current_profile_id", _currentProfileId);
                }
            }
        }

        public FinanceDatabase GetFinanceDatabase(string profileId)
        {
            var path = Path.Combine("D:/Шарага/С#/db", $"{profileId}_finance.db3");
            return new FinanceDatabase(path);
        }

        public CategoryDatabase GetCategoryDatabase(string profileId)
        {
            var path = Path.Combine("D:/Шарага/С#/db", $"{profileId}_categories.db3");
            return new CategoryDatabase(path);
        }

        public BudgetDatabase GetBudgetDatabase(string profileId)
        {
            var path = Path.Combine("D:/Шарага/С#/db", $"{profileId}_budgets.db3");
            return new BudgetDatabase(path);
        }

        public AssetDatabase GetAssetDatabase(string profileId)
        {
            var path = Path.Combine("D:/Шарага/С#/db", $"{profileId}_assets.db3");
            return new AssetDatabase(path);
        }

        public DebtDatabase GetDebtDatabase(string profileId)
        {
            var path = Path.Combine("D:/Шарага/С#/db", $"{profileId}_debts.db3");
            return new DebtDatabase(path);
        }

        public void UpdateProfileStatistics(int tasksCount, int notesCount, TimeSpan trackedTime)
        {
            var profile = GetCurrentProfile();
            if (profile != null)
            {
                profile.TotalTasks = tasksCount;
                profile.TotalNotes = notesCount;
                profile.TotalTrackedTime = trackedTime;
                UpdateProfile(profile);
            }
        }
    }
}