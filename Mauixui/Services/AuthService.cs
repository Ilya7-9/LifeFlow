using SQLite;
using Mauixui.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mauixui.Services
{
    public class AuthService
    {
        private readonly MainDatabase _database;

        public bool IsLoggedIn()
        {
            try
            {
                var profileId = Preferences.Get("current_profile_id", "");
                return !string.IsNullOrEmpty(profileId);
            }
            catch
            {
                return false;
            }
        }

        public AuthService()
        {
            _database = MainDatabase.Instance;
        }

        // Конструктор с параметром для совместимости
        public AuthService(ProfileService profileService)
        {
            _database = MainDatabase.Instance;
        }

        public async Task<(bool success, string message, UserProfile profile)> LoginAsync(
            string email, string password)
        {
            try
            {
                var profile = await _database.GetProfileByEmailAsync(email);

                if (profile == null)
                    return (false, "Пользователь не найден", null);

                if (!profile.IsActive)
                    return (false, "Профиль деактивирован", null);

                if (profile.Password != password)
                    return (false, "Неверный пароль", null);

                // Обновляем время последнего входа
                profile.LastLogin = DateTime.Now;
                await _database.UpdateProfileAsync(profile);

                return (true, "Вход успешен", profile);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка входа: {ex.Message}", null);
            }
        }

        public async Task<(bool success, string message, UserProfile profile)> RegisterAsync(
            string email, string password, string name, string avatar = "👤")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, "Некорректный email", null);

                if (string.IsNullOrWhiteSpace(password))
                    return (false, "Пароль не может быть пустым", null);

                if (string.IsNullOrWhiteSpace(name))
                    return (false, "Имя не может быть пустым", null);

                // Проверяем уникальность email
                if (await _database.CheckEmailExistsAsync(email))
                    return (false, "Пользователь с таким email уже существует", null);

                var profile = await _database.CreateProfileAsync(name, email, password, avatar);
                return (true, "Регистрация успешна", profile);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка регистрации: {ex.Message}", null);
            }
        }

        public async Task<(bool success, string message)> ChangePasswordAsync(
            string email, string currentPassword, string newPassword)
        {
            var loginResult = await LoginAsync(email, currentPassword);
            if (!loginResult.success)
                return (false, loginResult.message);

            if (string.IsNullOrWhiteSpace(newPassword))
                return (false, "Новый пароль не может быть пустым");

            loginResult.profile.Password = newPassword;
            await _database.UpdateProfileAsync(loginResult.profile);

            return (true, "Пароль успешно изменен");
        }

        public async Task<(bool success, string message)> ChangePasswordByProfileIdAsync(
            string profileId, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                    return (false, "Новый пароль не может быть пустым");

                var profile = await _database.GetProfileAsync(profileId);
                if (profile == null)
                    return (false, "Профиль не найден");

                profile.Password = newPassword;
                await _database.UpdateProfileAsync(profile);

                return (true, "Пароль успешно изменен");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка смены пароля: {ex.Message}");
            }
        }

        // Синхронные версии для совместимости
        public (bool success, string message, UserProfile profile) Login(
            string email, string password)
        {
            try
            {
                var task = LoginAsync(email, password);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка входа: {ex.Message}", null);
            }
        }

        public (bool success, string message, UserProfile profile) Register(
            string email, string password, string name, string avatar = "👤")
        {
            try
            {
                var task = RegisterAsync(email, password, name, avatar);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка регистрации: {ex.Message}", null);
            }
        }

        public void Logout()
        {
            Preferences.Remove("current_profile_id");
        }

    }
}