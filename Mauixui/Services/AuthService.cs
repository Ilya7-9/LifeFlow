using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mauixui.Models;
using Microsoft.Maui.Storage;

namespace Mauixui.Services
{
    public class AuthService
    {
        private readonly ProfileService _profileService;

        public AuthService(ProfileService profileService)
        {
            _profileService = profileService;
        }

        // СДЕЛАЕМ МЕТОД ПУБЛИЧНЫМ
        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // Остальные методы без изменений...
        public (bool success, string message) Register(string email, string password, string name, string avatar = "👤")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, "Некорректный email");

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return (false, "Пароль должен содержать минимум 6 символов");

                if (string.IsNullOrWhiteSpace(name))
                    return (false, "Имя не может быть пустым");

                var profiles = _profileService.GetProfiles();
                if (profiles.Any(p => p.Email?.ToLower() == email.ToLower()))
                    return (false, "Пользователь с таким email уже существует");

                var profile = new UserProfile
                {
                    Email = email.Trim().ToLower(),
                    PasswordHash = HashPassword(password),
                    Name = name.Trim(),
                    Avatar = avatar,
                    CreatedAt = DateTime.Now,
                    LastLogin = DateTime.Now,
                    IsActive = true
                };

                _profileService.AddProfile(profile);
                _profileService.SetCurrentProfile(profile);

                return (true, "Регистрация успешна");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка регистрации: {ex.Message}");
            }
        }

        public (bool success, string message, UserProfile profile) Login(string email, string password)
        {
            try
            {
                var profiles = _profileService.GetProfiles();
                var profile = profiles.FirstOrDefault(p =>
                    p.Email?.ToLower() == email.ToLower() &&
                    p.IsActive);

                if (profile == null)
                    return (false, "Пользователь не найден", null);

                if (!VerifyPassword(password, profile.PasswordHash))
                    return (false, "Неверный пароль", null);

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

        public (bool success, string message) ChangePassword(string email, string currentPassword, string newPassword)
        {
            try
            {
                var loginResult = Login(email, currentPassword);
                if (!loginResult.success)
                    return (false, loginResult.message);

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return (false, "Новый пароль должен содержать минимум 6 символов");

                var profile = loginResult.profile;
                profile.PasswordHash = HashPassword(newPassword);
                _profileService.UpdateProfile(profile);

                return (true, "Пароль успешно изменен");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка смены пароля: {ex.Message}");
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            return HashPassword(password) == storedHash;
        }

        public bool IsLoggedIn()
        {
            var currentProfile = _profileService.GetCurrentProfile();
            return currentProfile != null && !string.IsNullOrEmpty(currentProfile.Email);
        }

        public void Logout()
        {
            Preferences.Remove("current_profile_id");
        }
    }
}