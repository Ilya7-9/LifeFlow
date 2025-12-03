using SQLite;
using Mauixui.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mauixui.Services
{
    public class CredentialsService
    {
        private SQLiteAsyncConnection _database;
        private bool _isInitialized = false;
        private readonly string _dbDirectory = @"D:/Шарага/С#/db";
        private readonly string _dbPath;

        public CredentialsService()
        {
            _dbPath = Path.Combine(_dbDirectory, "credentials.db3");
            Console.WriteLine($"🔧 CredentialsService создан. Путь: {_dbPath}");
            _ = InitializeDatabaseAsync();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                if (_isInitialized)
                {
                    Console.WriteLine("✅ БД уже инициализирована");
                    return;
                }

                Console.WriteLine($"🔄 Начинаем инициализацию БД...");

                // Создаем директорию если не существует
                Directory.CreateDirectory(_dbDirectory);
                Console.WriteLine($"📁 Директория создана/проверена: {_dbDirectory}");

                _database = new SQLiteAsyncConnection(_dbPath);
                Console.WriteLine($"🔗 Подключение к БД установлено");

                // Создаем таблицу
                var result = await _database.CreateTableAsync<UserCredentials>();
                Console.WriteLine($"📊 Таблица создана. Результат: {result}");

                _isInitialized = true;
                Console.WriteLine($"✅ Credentials БД успешно инициализирована: {_dbPath}");

                // Проверяем существование файла
                if (File.Exists(_dbPath))
                {
                    var fileInfo = new FileInfo(_dbPath);
                    Console.WriteLine($"✅ Файл существует. Размер: {fileInfo.Length} байт");

                    // Показываем сколько записей в таблице
                    var count = await _database.Table<UserCredentials>().CountAsync();
                    Console.WriteLine($"📊 Записей в таблице: {count}");
                }
                else
                {
                    Console.WriteLine($"❌ Файл не создан: {_dbPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ КРИТИЧЕСКАЯ ОШИБКА инициализации БД: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            }
        }

        // Метод для принудительной проверки инициализации
        private async Task EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Console.WriteLine("⚠️ БД не инициализирована, пытаемся инициализировать...");
                await InitializeDatabaseAsync();
            }
        }

        public async Task<UserCredentials> GetCredentialsAsync(string profileId)
        {
            try
            {
                await EnsureInitialized();

                if (_database == null)
                {
                    Console.WriteLine("❌ _database is null в GetCredentialsAsync");
                    return null;
                }

                if (string.IsNullOrEmpty(profileId))
                {
                    Console.WriteLine("❌ profileId is null or empty");
                    return null;
                }

                Console.WriteLine($"🔍 Поиск учетных данных для profileId: {profileId}");

                var credentials = await _database.Table<UserCredentials>()
                    .Where(x => x.ProfileId == profileId)
                    .FirstOrDefaultAsync();

                Console.WriteLine(credentials == null
                    ? $"❌ Учетные данные не найдены для {profileId}"
                    : $"✅ Найдены учетные данные: {credentials.Email}");

                return credentials;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в GetCredentialsAsync: {ex.Message}");
                return null;
            }
        }

        // ДОБАВЛЯЕМ НЕДОСТАЮЩИЕ МЕТОДЫ:

        public async Task<UserCredentials> GetCredentialsByEmailAsync(string email)
        {
            try
            {
                await EnsureInitialized();

                if (_database == null)
                {
                    Console.WriteLine("❌ _database is null в GetCredentialsByEmailAsync");
                    return null;
                }

                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine("❌ email is null or empty");
                    return null;
                }

                Console.WriteLine($"🔍 Поиск учетных данных для email: {email}");

                var credentials = await _database.Table<UserCredentials>()
                    .Where(x => x.Email == email)
                    .FirstOrDefaultAsync();

                Console.WriteLine(credentials == null
                    ? $"❌ Учетные данные не найдены для {email}"
                    : $"✅ Найдены учетные данные: ProfileId={credentials.ProfileId}");

                return credentials;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в GetCredentialsByEmailAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdatePasswordAsync(string profileId, string newPassword)
        {
            try
            {
                await EnsureInitialized();

                if (_database == null)
                {
                    Console.WriteLine("❌ _database is null в UpdatePasswordAsync");
                    return false;
                }

                if (string.IsNullOrEmpty(profileId))
                {
                    Console.WriteLine("❌ profileId is null or empty");
                    return false;
                }

                Console.WriteLine($"🔄 Обновление пароля для profileId: {profileId}");

                var credentials = await GetCredentialsAsync(profileId);
                if (credentials != null)
                {
                    credentials.PasswordHash = newPassword ?? "";
                    credentials.UpdatedAt = DateTime.Now;
                    await _database.UpdateAsync(credentials);

                    Console.WriteLine($"✅ Пароль успешно обновлен для {profileId}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Учетные данные не найдены для {profileId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в UpdatePasswordAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveCredentialsAsync(string profileId, string email, string password)
        {
            try
            {
                await EnsureInitialized();

                if (_database == null)
                {
                    Console.WriteLine("❌ _database is null в SaveCredentialsAsync");
                    return false;
                }

                if (string.IsNullOrEmpty(profileId))
                {
                    Console.WriteLine("❌ profileId is null or empty");
                    return false;
                }

                Console.WriteLine($"💾 Сохранение учетных данных: ProfileId={profileId}, Email={email}");

                var existing = await _database.Table<UserCredentials>()
                    .Where(x => x.ProfileId == profileId)
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    Console.WriteLine($"🔄 Обновление существующих учетных данных");
                    existing.Email = email ?? "";
                    existing.PasswordHash = password ?? "";
                    existing.UpdatedAt = DateTime.Now;
                    await _database.UpdateAsync(existing);
                }
                else
                {
                    Console.WriteLine($"🆕 Создание новых учетных данных");
                    var credentials = new UserCredentials
                    {
                        ProfileId = profileId,
                        Email = email ?? "",
                        PasswordHash = password ?? "",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    await _database.InsertAsync(credentials);
                }

                // Проверяем что запись сохранилась
                var saved = await GetCredentialsAsync(profileId);
                var success = saved != null;

                Console.WriteLine(success
                    ? $"✅ Учетные данные успешно сохранены для профиля: {profileId}"
                    : $"❌ Не удалось сохранить учетные данные для профиля: {profileId}");

                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения учетных данных: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCredentialsAsync(string profileId)
        {
            try
            {
                await EnsureInitialized();

                if (_database == null) return false;

                var credentials = await GetCredentialsAsync(profileId);
                if (credentials != null)
                {
                    await _database.DeleteAsync(credentials);
                    Console.WriteLine($"✅ Учетные данные удалены для {profileId}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка удаления учетных данных: {ex.Message}");
                return false;
            }
        }

        public async Task<List<UserCredentials>> GetAllCredentialsAsync()
        {
            try
            {
                await EnsureInitialized();

                if (_database == null) return new List<UserCredentials>();

                return await _database.Table<UserCredentials>().ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения всех учетных данных: {ex.Message}");
                return new List<UserCredentials>();
            }
        }

        public async Task<bool> VerifyPasswordAsync(string profileId, string password)
        {
            try
            {
                var credentials = await GetCredentialsAsync(profileId);
                return credentials?.PasswordHash == password;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки пароля: {ex.Message}");
                return false;
            }
        }

        // Метод для отладки - показать все учетные данные
        public async Task DebugShowAllCredentials()
        {
            try
            {
                await EnsureInitialized();

                if (_database == null)
                {
                    Console.WriteLine("❌ _database is null в DebugShowAllCredentials");
                    return;
                }

                var allCredentials = await _database.Table<UserCredentials>().ToListAsync();
                Console.WriteLine($"📋 ВСЕ УЧЕТНЫЕ ДАННЫЕ ({allCredentials.Count} записей):");

                foreach (var cred in allCredentials)
                {
                    Console.WriteLine($"   👤 ProfileId: {cred.ProfileId}");
                    Console.WriteLine($"   📧 Email: {cred.Email}");
                    Console.WriteLine($"   🔑 Password: {cred.PasswordHash}");
                    Console.WriteLine($"   📅 Created: {cred.CreatedAt}");
                    Console.WriteLine($"   🔄 Updated: {cred.UpdatedAt}");
                    Console.WriteLine($"   ─────────────────────");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в DebugShowAllCredentials: {ex.Message}");
            }
        }
    }
}