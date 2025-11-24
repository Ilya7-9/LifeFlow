using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Mauixui.Views;
using Mauixui.Models;
using Mauixui.Services;

namespace Mauixui
{
    public partial class MainPage : ContentPage
    {
        private Button _currentActiveButton;
        private ProfileService _profileService;
        private UserProfile _currentProfile;

        public MainPage()
        {
            InitializeComponent();

            _profileService = new ProfileService();
            _currentProfile = _profileService.GetCurrentProfile();

            _currentActiveButton = HomeButton;
            SetActiveButton(HomeButton);

            LoadView(new ProfileView());
            LoadProfileSettings();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            RefreshProfileInfo();
        }

        private void LoadProfileSettings()
        {
            ThemeSwitch.IsToggled = _currentProfile.Theme == AppTheme.Dark ||
                                   (Application.Current.UserAppTheme == AppTheme.Dark);
            RefreshProfileInfo();
        }

        private void RefreshProfileInfo()
        {
            _currentProfile = _profileService.GetCurrentProfile();

            ProfileAvatarLabel.Text = _currentProfile.Avatar;
            ProfileNameLabel.Text = _currentProfile.Name;
            ProfileStatsLabel.Text = $"Задачи: {_currentProfile.TotalTasks} | Заметки: {_currentProfile.TotalNotes}";

            UpdateButtonColors();
        }

        private void LoadView(View view)
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(view);
        }

        private void SetActiveButton(Button activeButton)
        {
            if (_currentActiveButton != null)
            {
                _currentActiveButton.BackgroundColor = Color.FromArgb("#00FFFFFF");
                _currentActiveButton.TextColor = Application.Current.UserAppTheme == AppTheme.Dark ?
                    Color.FromArgb("#B9BBBE") : Color.FromArgb("#747F8D");
            }

            var accentColor = _currentProfile?.AccentColor ?? "#5865F2";
            activeButton.BackgroundColor = Color.FromArgb(accentColor);
            activeButton.TextColor = Color.FromArgb("#FFFFFF");

            _currentActiveButton = activeButton;
        }

        private void OnHomeClicked(object sender, EventArgs e)
        {
            SetActiveButton(HomeButton);
            LoadView(new ProfileView());
        }

        //private void OnProfileClicked(object sender, EventArgs e)
        //{
        //    SetActiveButton(ProfileButton);
        //    LoadView(new ProfileView());
        //}

        private void OnFinanceClicked(object sender, EventArgs e)
        {
            SetActiveButton(FinanceButton);
            LoadView(new FinanceView());
        }

        private void OnTrackClicked(object sender, EventArgs e)
        {
            SetActiveButton(TrackButton);
            LoadView(new TrackerView());
        }

        //private void OnNotesClicked(object sender, EventArgs e)
        //{
        //    SetActiveButton(NotesButton);
        //    LoadView(new NotesView());
        //}


        private async void OnProfileMenuClicked(object sender, EventArgs e)
        {
            var action = await DisplayActionSheet("Управление профилями", "Отмена", null,
                "Сменить профиль", "Создать профиль", "Редактировать профиль", "Статистика");

            switch (action)
            {
                case "Сменить профиль":
                    await ShowProfileSelection();
                    break;
                case "Создать профиль":
                    await CreateNewProfile();
                    break;
                case "Редактировать профиль":
                    await EditCurrentProfile();
                    break;
                case "Статистика":
                    await ShowProfileStatistics();
                    break;
            }
        }

        private async Task ShowProfileSelection()
        {
            var profiles = _profileService.GetProfiles();
            var profileNames = profiles.Select(p => $"{p.Avatar} {p.Name}").ToArray();

            var selected = await DisplayActionSheet("Выберите профиль", "Отмена", null, profileNames);

            if (selected != "Отмена" && !string.IsNullOrEmpty(selected))
            {
                var selectedProfile = profiles.FirstOrDefault(p => $"{p.Avatar} {p.Name}" == selected);
                if (selectedProfile != null)
                {
                    _profileService.SetCurrentProfile(selectedProfile);
                    await _profileService.UpdateAllProfilesStatsAsync();
                    RefreshProfileInfo();

                    if (_currentActiveButton == HomeButton)
                        LoadView(new ProfileView());
                    //else if (_currentActiveButton == ProfileButton)
                    //    LoadView(new ProfileView());
                    else if (_currentActiveButton == FinanceButton)
                        LoadView(new FinanceView());
                    else if (_currentActiveButton == TrackButton)
                        LoadView(new TrackerView());
                    //else if (_currentActiveButton == NotesButton)
                    //    LoadView(new TrackerView());

                    await DisplayAlert("Успех", $"Профиль {selectedProfile.Name} активирован", "OK");
                }
            }
        }

        // УБЕРИТЕ ДУБЛИРУЮЩИЙ МЕТОД - ОСТАВЬТЕ ТОЛЬКО ОДИН
        public async void RefreshGlobalStatistics()
        {
            await _profileService.UpdateAllProfilesStatsAsync();
            RefreshProfileInfo();
        }

        private async Task CreateNewProfile()
        {
            var name = await DisplayPromptAsync("Создать профиль", "Введите имя профиля:", "Создать", "Отмена", "Новый профиль");

            if (!string.IsNullOrWhiteSpace(name) && name != "Отмена")
            {
                var avatar = await DisplayActionSheet("Выберите аватар", "Отмена", null,
                    "👤", "👨", "👩", "🧑", "👨‍💻", "👩‍💻", "🎮", "📚", "⚡");

                if (avatar != "Отмена")
                {
                    var newProfile = _profileService.CreateProfile(name, avatar);
                    RefreshProfileInfo();
                    await DisplayAlert("Успех", $"Профиль {name} создан", "OK");
                }
            }
        }

        private async Task EditCurrentProfile()
        {
            var action = await DisplayActionSheet("Редактировать профиль", "Отмена", "Удалить",
                "Изменить имя", "Сменить аватар", "Цвет темы");

            switch (action)
            {
                case "Изменить имя":
                    await ChangeProfileName();
                    break;
                case "Сменить аватар":
                    await ChangeProfileAvatar();
                    break;
                case "Цвет темы":
                    await ChangeAccentColor();
                    break;
                case "Удалить":
                    await DeleteCurrentProfile();
                    break;
            }
        }

        private async Task ChangeProfileName()
        {
            var newName = await DisplayPromptAsync("Изменить имя", "Введите новое имя:", "Сохранить", "Отмена", _currentProfile.Name);

            if (!string.IsNullOrWhiteSpace(newName) && newName != "Отмена")
            {
                _currentProfile.Name = newName;
                _profileService.UpdateProfile(_currentProfile);
                RefreshProfileInfo();
            }
        }

        private async Task ChangeProfileAvatar()
        {
            var avatar = await DisplayActionSheet("Выберите аватар", "Отмена", null,
                "👤", "👨", "👩", "🧑", "👨‍💻", "👩‍💻", "🎮", "📚", "⚡", "🌟", "🎯", "💼");

            if (avatar != "Отмена")
            {
                _currentProfile.Avatar = avatar;
                _profileService.UpdateProfile(_currentProfile);
                RefreshProfileInfo();
            }
        }

        private async Task ChangeAccentColor()
        {
            var colors = new[]
            {
                ("Синий", "#5865F2"),
                ("Зеленый", "#23A55A"),
                ("Желтый", "#F0B232"),
                ("Красный", "#F23F43"),
                ("Фиолетовый", "#9B59B6"),
                ("Розовый", "#E91E63"),
                ("Бирюзовый", "#1ABC9C")
            };

            var colorNames = colors.Select(c => c.Item1).ToArray();
            var selected = await DisplayActionSheet("Цвет акцента", "Отмена", null, colorNames);

            if (selected != "Отмена")
            {
                var selectedColor = colors.FirstOrDefault(c => c.Item1 == selected);
                if (selectedColor != default)
                {
                    _currentProfile.AccentColor = selectedColor.Item2;
                    _profileService.UpdateProfile(_currentProfile);
                    RefreshProfileInfo();
                }
            }
        }

        private async Task DeleteCurrentProfile()
        {
            var confirm = await DisplayAlert("Удалить профиль",
                $"Вы уверены, что хотите удалить профиль {_currentProfile.Name}? Это действие нельзя отменить.",
                "Удалить", "Отмена");

            if (confirm)
            {
                _profileService.DeleteProfile(_currentProfile.Id);
                RefreshProfileInfo();
                await DisplayAlert("Успех", "Профиль удален", "OK");
            }
        }

        private async Task ShowProfileStatistics()
        {
            var currentProfile = _profileService.GetCurrentProfile();

            var stats = "📊 Статистика профиля: " + currentProfile.Name + "\n\n" +
                       "📅 Создан: " + currentProfile.CreatedAt.ToString("dd.MM.yyyy") + "\n" +
                       "✅ Задач выполнено: " + currentProfile.TotalTasks + "\n" +
                       "📝 Заметок создано: " + currentProfile.TotalNotes + "\n" +
                       "⏱️ Отслежено времени: " + currentProfile.TotalTrackedTime.ToString(@"hh\:mm\:ss") + "\n\n" +
                       "🎨 Настройки:\n" +
                       "• Тема: " + GetThemeName(currentProfile.Theme) + "\n" +
                       "• Цвет акцента: " + currentProfile.AccentColor;

            await DisplayAlert("Статистика профиля", stats, "OK");
        }

        private string GetThemeName(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Dark => "Тёмная",
                AppTheme.Light => "Светлая",
                AppTheme.Unspecified => "Системная",
                _ => "Неизвестно"
            };
        }

        public void UpdateProfileStatistics(int tasksCount, int notesCount, TimeSpan trackedTime)
        {
            _profileService.UpdateProfileStatistics(tasksCount, notesCount, trackedTime);
            RefreshProfileInfo();
        }

        private void OnThemeSwitchToggled(object sender, ToggledEventArgs e)
        {
            var newTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
            Application.Current.UserAppTheme = newTheme;

            _currentProfile.Theme = newTheme;
            _profileService.UpdateProfile(_currentProfile);

            UpdateButtonColors();
        }

        private void UpdateButtonColors()
        {
            var currentActive = _currentActiveButton;
            SetActiveButton(currentActive);
        }
    }
}