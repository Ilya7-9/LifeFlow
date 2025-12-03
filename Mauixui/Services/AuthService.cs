using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mauixui.Models;
using Microsoft.Maui.Storage;

namespace Mauixui.Services
{
    public class AuthService
    {
        private readonly ProfileService _profileService;
        private readonly CredentialsService _credentialsService;

        public AuthService(ProfileService profileService)
        {
            _profileService = profileService;
            _credentialsService = new CredentialsService();
        }

        // СТАРЫЕ СИНХРОННЫЕ МЕТОДЫ ДЛЯ СОВМЕСТИМОСТИ
        public (bool success, string message, UserProfile profile) Login(string email, string password)
        {
            try
            {
                // Простой синхронный вызов асинхронного метода
                var task = LoginAsync(email, password);
                task.Wait(); // Блокируем выполнение
                return task.Result;
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка входа: {ex.Message}", null);
            }
        }

        public (bool success, string message) Register(string email, string password, string name, string avatar = "👤")
        {
            try
            {
                var task = RegisterAsync(email, password, name, avatar);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка регистрации: {ex.Message}");
            }
        }

        public (bool success, string message) ChangePassword(string email, string currentPassword, string newPassword)
        {
            try
            {
                var task = ChangePasswordAsync(email, currentPassword, newPassword);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка смены пароля: {ex.Message}");
            }
        }

        // НОВЫЕ АСИНХРОННЫЕ МЕТОДЫ
        public async Task<(bool success, string message)> RegisterAsync(string email, string password, string name, string avatar = "👤")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, "Некорректный email");

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return (false, "Пароль должен содержать минимум 6 символов");

                if (string.IsNullOrWhiteSpace(name))
                    return (false, "Имя не может быть пустым");

                // Проверяем существование email
                var existingCredentials = await _credentialsService.GetCredentialsByEmailAsync(email);
                if (existingCredentials != null)
                    return (false, "Пользователь с таким email уже существует");

                // Создаем профиль
                var profile = new UserProfile
                {
                    Name = name.Trim(),
                    Avatar = avatar,
                    CreatedAt = DateTime.Now,
                    LastLogin = DateTime.Now,
                    IsActive = true
                };

                _profileService.AddProfile(profile);

                // Сохраняем учетные данные С EMAIL
                var success = await _credentialsService.SaveCredentialsAsync(profile.Id, email, password);
                if (!success)
                    return (false, "Ошибка сохранения учетных данных");

                _profileService.SetCurrentProfile(profile);
                return (true, "Регистрация успешна");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка регистрации: {ex.Message}");
            }
        }

        public async Task<(bool success, string message, UserProfile profile)> LoginAsync(string email, string password)
        {
            try
            {
                // Ищем учетные данные по email
                var credentials = await _credentialsService.GetCredentialsByEmailAsync(email);
                if (credentials == null)
                    return (false, "Пользователь не найден", null);

                // Проверяем пароль
                if (credentials.PasswordHash != password)
                    return (false, "Неверный пароль", null);

                // Находим профиль
                var profile = _profileService.GetProfiles()
                    .FirstOrDefault(p => p.Id == credentials.ProfileId && p.IsActive);

                if (profile == null)
                    return (false, "Профиль не найден", null);

                // Обновляем время входа
                profile.LastLogin = DateTime.Now;
                _profileService.UpdateProfile(profile);
                _profileService.SetCurrentProfile(profile);

                return (true, "Вход успешен", profile);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка входа: {ex.Message}", null);
            }
        }

        public async Task<(bool success, string message)> ChangePasswordAsync(string email, string currentPassword, string newPassword)
        {
            try
            {
                var loginResult = await LoginAsync(email, currentPassword);
                if (!loginResult.success)
                    return (false, loginResult.message);

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return (false, "Новый пароль должен содержать минимум 6 символов");

                var credentials = await _credentialsService.GetCredentialsByEmailAsync(email);
                if (credentials == null)
                    return (false, "Учетные данные не найдены");

                var success = await _credentialsService.UpdatePasswordAsync(credentials.ProfileId, newPassword);
                return success ?
                    (true, "Пароль успешно изменен") :
                    (false, "Ошибка изменения пароля");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка смены пароля: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> ChangePasswordByProfileIdAsync(string profileId, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return (false, "Новый пароль должен содержать минимум 6 символов");

                var success = await _credentialsService.UpdatePasswordAsync(profileId, newPassword);
                return success ?
                    (true, "Пароль успешно изменен") :
                    (false, "Ошибка изменения пароля");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка смены пароля: {ex.Message}");
            }
        }

        public bool IsLoggedIn()
        {
            var currentProfile = _profileService.GetCurrentProfile();
            return currentProfile != null;
        }

        public void Logout()
        {
            Preferences.Remove("current_profile_id");
        }

        // Получить email текущего пользователя
        public async Task<string> GetCurrentUserEmailAsync()
        {
            var currentProfile = _profileService.GetCurrentProfile();
            if (currentProfile == null) return null;

            var credentials = await _credentialsService.GetCredentialsAsync(currentProfile.Id);
            return credentials?.Email;
        }
    }
}