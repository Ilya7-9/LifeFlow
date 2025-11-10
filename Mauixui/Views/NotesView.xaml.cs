using Microsoft.Maui.Controls;
using Mauixui.Models;
using Mauixui.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mauixui.Views
{
    public partial class NotesView : ContentView, INotifyPropertyChanged
    {
        private List<NoteItem> allNotes = new();
        private NoteDatabase _database;
        private TaskDatabase _taskDatabase;
        private NoteItem currentNote;
        private NoteItem selectedNote;
        private ProfileService _profileService;
        private string _currentProfileId;

        // Пагинация
        private int currentPage = 1;
        private int pageSize = 20;
        private int totalPages = 1;

        public ObservableCollection<string> CurrentTags { get; } = new ObservableCollection<string>();
        public ICommand RefreshNotesCommand { get; }

        // Свойства для привязки
        public bool ShowPagination => totalPages > 1;
        public string PageInfo => $"Страница {currentPage} из {totalPages}";
        public bool CanGoToFirstPage => currentPage > 1;
        public bool CanGoToPreviousPage => currentPage > 1;
        public bool CanGoToNextPage => currentPage < totalPages;
        public bool CanGoToLastPage => currentPage < totalPages;

        public int CurrentPage
        {
            get => currentPage;
            set
            {
                if (value >= 1 && value <= totalPages)
                {
                    currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                    OnPropertyChanged(nameof(PageInfo));
                    OnPropertyChanged(nameof(CanGoToFirstPage));
                    OnPropertyChanged(nameof(CanGoToPreviousPage));
                    OnPropertyChanged(nameof(CanGoToNextPage));
                    OnPropertyChanged(nameof(CanGoToLastPage));
                    RenderNotes();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public NotesView()
        {
            InitializeComponent();
            InitializeDatabase();

            _profileService = new ProfileService();
            var currentProfile = _profileService.GetCurrentProfile();
            _currentProfileId = currentProfile.Id;

            // ИСПОЛЬЗУЕМ БАЗУ ДАННЫХ КОНКРЕТНОГО ПРОФИЛЯ
            _database = _profileService.GetNoteDatabase(_currentProfileId);

            RefreshNotesCommand = new Command(async () => await RefreshNotes());

            LoadNotes();
            BindingContext = this;
        }

        private void InitializeDatabase()
        {
            string dbPath = Path.Combine("D:/Шарага/С#/db", "notes.db3");
            _database = new NoteDatabase(dbPath);

            string taskDbPath = Path.Combine("D:/Шарага/С#/db", "tasks.db3");
            _taskDatabase = new TaskDatabase(taskDbPath);
        }

        private async Task RefreshNotes()
        {
            await LoadNotes();
            if (NotesRefreshView != null)
                NotesRefreshView.IsRefreshing = false;
        }

        private async Task LoadNotes()
        {
            try
            {
                allNotes = await _database.GetNotesAsync(_currentProfileId);
                allNotes = allNotes.OrderByDescending(n => n.IsPinned)
                                  .ThenByDescending(n => n.UpdatedAt)
                                  .ToList();

                CalculatePagination();
                RenderNotes();
                UpdateProfileStats();
            }
            catch (Exception ex)
            {
                await ShowAlert("Ошибка", $"Не удалось загрузить заметки: {ex.Message}");
            }
        }

        private void CalculatePagination()
        {
            totalPages = (int)Math.Ceiling((double)allNotes.Count / pageSize);
            if (currentPage > totalPages && totalPages > 0)
                currentPage = totalPages;
            else if (totalPages == 0)
                currentPage = 1;

            UpdatePaginationProperties();
        }

        private void UpdatePaginationProperties()
        {
            OnPropertyChanged(nameof(ShowPagination));
            OnPropertyChanged(nameof(PageInfo));
            OnPropertyChanged(nameof(CanGoToFirstPage));
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
            OnPropertyChanged(nameof(CanGoToLastPage));
            OnPropertyChanged(nameof(CurrentPage));
        }

        private Color GetNoteColor(string color)
        {
            return color switch
            {
                "Blue" => Color.FromArgb("#4A6FFF"),
                "Green" => Color.FromArgb("#23D160"),
                "Purple" => Color.FromArgb("#8B5CF6"),
                "Pink" => Color.FromArgb("#EC4899"),
                "Yellow" => Color.FromArgb("#F59E0B"),
                "Gray" => Color.FromArgb("#6B7280"),
                _ => Color.FromArgb("#40444B")
            };
        }

        private void ClearEditor()
        {
            currentNote = null;
            selectedNote = null;
            if (TitleEntry != null) TitleEntry.Text = "";
            if (ContentEditor != null) ContentEditor.Text = "";
            CurrentTags.Clear();
            if (SaveButton != null) SaveButton.Text = "💾 Сохранить";
            if (DeleteButton != null) DeleteButton.IsVisible = false;
        }

        // ===== МЕТОДЫ ДЛЯ АЛЕРТОВ =====

        private async Task<bool> ShowConfirmationAlert(string title, string message)
        {
            if (Application.Current?.MainPage != null)
                return await Application.Current.MainPage.DisplayAlert(title, message, "Удалить", "Отмена");
            return false;
        }

        private async Task ShowAlert(string title, string message)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }

        private async Task<string> DisplayPromptAsync(string title, string message, string accept, string cancel)
        {
            if (Application.Current?.MainPage != null)
                return await Application.Current.MainPage.DisplayPromptAsync(title, message, accept, cancel, maxLength: 20);
            return null;
        }

        private async Task<string> DisplayActionSheet(string title, string cancel, string destruction, params string[] buttons)
        {
            if (Application.Current?.MainPage != null)
                return await Application.Current.MainPage.DisplayActionSheet(title, cancel, destruction, buttons);
            return null;
        }

        // ===== СИНХРОНИЗАЦИЯ С ЗАДАЧАМИ =====

        private async Task SyncNotesWithTasks()
        {
            try
            {
                // ВРЕМЕННОЕ РЕШЕНИЕ - отключаем синхронизацию
                // или используем простую логику
                var notesWithTaskTag = allNotes.Where(n => !string.IsNullOrEmpty(n.TagsString) && n.TagsString.Contains("#задача")).ToList();

                foreach (var note in notesWithTaskTag)
                {
                    await ConvertNoteToTask(note);
                    // Временно отключаем это свойство
                    // note.IsConvertedToTask = true;
                    await _database.SaveNoteAsync(note);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка синхронизации: {ex.Message}");
            }
        }

        private async Task ConvertNoteToTask(NoteItem note)
        {
            var task = new TaskItem
            {
                Title = note.Title,
                Description = note.Content,
                CreatedAt = DateTime.Now,
                Deadline = null,
                IsCompleted = false,
                IsFavorite = false,
                Category = "Из заметки",
                Priority = "Средний",
                Source = "note"
            };

            await _taskDatabase.SaveTaskAsync(task);
        }

        private void FilterTasks(object sender, EventArgs e)
        {
            allNotes = allNotes.Where(n => n.HasTaskTag).ToList();
            CalculatePagination();
            RenderNotes();
        }

        // ===== ОСНОВНЫЕ ФУНКЦИИ ЗАМЕТОК =====

        private void RenderNotes()
        {
            if (NotesContainer == null) return;

            NotesContainer.Children.Clear();

            if (!allNotes.Any())
            {
                var emptyLabel = new Label
                {
                    Text = "📝\nЗаметок пока нет\n\nСоздайте первую заметку!",
                    FontSize = 16,
                    TextColor = Color.FromArgb("#888888"),
                    HorizontalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                NotesContainer.Children.Add(emptyLabel);
                return;
            }

            var notesToShow = allNotes.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

            foreach (var note in notesToShow)
            {
                var noteFrame = CreateNoteFrame(note);
                NotesContainer.Children.Add(noteFrame);
            }

            NotesContainer.Children.Add(new BoxView
            {
                HeightRequest = 20,
                Color = Color.FromArgb("#00FFFFFF")
            });
        }

        private Frame CreateNoteFrame(NoteItem note)
        {
            var frame = new Frame
            {
                BackgroundColor = GetNoteColor(note.Color),
                CornerRadius = 12,
                Padding = 15,
                HasShadow = true,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            // Заголовок и пин
            var titleLabel = new Label
            {
                Text = note.Title,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#FFFFFF"),
                LineBreakMode = LineBreakMode.TailTruncation
            };
            Grid.SetRow(titleLabel, 0);
            Grid.SetColumn(titleLabel, 0);

            var pinLabel = new Label
            {
                Text = "📌",
                FontSize = 12,
                IsVisible = note.IsPinned
            };
            Grid.SetRow(pinLabel, 0);
            Grid.SetColumn(pinLabel, 1);

            // Контент
            var contentLabel = new Label
            {
                Text = note.Preview,
                FontSize = 13,
                TextColor = Color.FromArgb("#CCCCCC"),
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 3
            };
            Grid.SetRow(contentLabel, 1);
            Grid.SetColumn(contentLabel, 0);
            Grid.SetColumnSpan(contentLabel, 2);

            // Теги, дата и маленькая пометка задачи
            var tagsLayout = new HorizontalStackLayout { Spacing = 5 };

            if (!string.IsNullOrEmpty(note.TagsString))
            {
                var tags = note.TagsString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in tags.Take(3))
                {
                    var tagFrame = new Frame
                    {
                        BackgroundColor = Color.FromArgb("#5865F2"),
                        CornerRadius = 8,
                        Padding = new Thickness(5, 2),
                        HasShadow = false
                    };
                    tagFrame.Content = new Label
                    {
                        Text = tag,
                        FontSize = 9,
                        TextColor = Color.FromArgb("#FFFFFF")
                    };
                    tagsLayout.Children.Add(tagFrame);
                }
            }

            // Маленькая пометка задачи справа
            var taskBadge = new Label
            {
                Text = "✅",
                FontSize = 10,
                // ИСПРАВЛЯЕМ HasTaskTag на простую проверку
                IsVisible = !string.IsNullOrEmpty(note.TagsString) && note.TagsString.Contains("#задача")
            };

            var dateLabel = new Label
            {
                Text = note.UpdateAt.ToString("dd.MM.yy"),
                FontSize = 10,
                TextColor = Color.FromArgb("#888888")
            };

            var bottomLayout = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            // Теги слева
            bottomLayout.Children.Add(tagsLayout);
            Grid.SetColumn(tagsLayout, 0);

            // Дата по центру
            bottomLayout.Children.Add(dateLabel);
            Grid.SetColumn(dateLabel, 1);

            // Пометка задачи справа (только иконка)
            bottomLayout.Children.Add(taskBadge);
            Grid.SetColumn(taskBadge, 2);

            Grid.SetRow(bottomLayout, 2);
            Grid.SetColumn(bottomLayout, 0);
            Grid.SetColumnSpan(bottomLayout, 2);

            grid.Children.Add(titleLabel);
            grid.Children.Add(pinLabel);
            grid.Children.Add(contentLabel);
            grid.Children.Add(bottomLayout);

            // Добавляем обработчик нажатия
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => OnNoteTapped(note);
            frame.GestureRecognizers.Add(tapGesture);

            frame.Content = grid;
            return frame;
        }

        private void OnNoteTapped(NoteItem note)
        {
            selectedNote = note;
            if (DeleteButton != null)
                DeleteButton.IsVisible = true;

            currentNote = note;
            if (TitleEntry != null)
                TitleEntry.Text = note.Title;
            if (ContentEditor != null)
                ContentEditor.Text = note.Content;
            CurrentTags.Clear();

            if (!string.IsNullOrEmpty(note.TagsString))
            {
                var tags = note.TagsString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in tags)
                    CurrentTags.Add(tag);
            }

            if (SaveButton != null)
                SaveButton.Text = "💾 Обновить";
        }

        private async void SaveNote(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TitleEntry?.Text) && string.IsNullOrWhiteSpace(ContentEditor?.Text))
                {
                    await ShowAlert("Ошибка", "Заметка не может быть пустой");
                    return;
                }

                var note = currentNote ?? new NoteItem();
                note.Title = TitleEntry?.Text?.Trim() ?? "Без названия";
                note.Content = ContentEditor?.Text?.Trim() ?? "";
                note.TagsString = CurrentTags.Any() ? string.Join(";", CurrentTags) : "";
                note.ProfileId = _currentProfileId;

                // UpdatedAt устанавливается автоматически в базе данных
                // CreatedAt устанавливается автоматически для новых заметок

                var result = await _database.SaveNoteAsync(note);

                if (result > 0)
                {
                    await LoadNotes();
                    ClearEditor();
                    await ShowAlert("Успех", "Заметка сохранена!");
                }
                else
                {
                    await ShowAlert("Ошибка", "Не удалось сохранить заметку");
                }
            }
            catch (Exception ex)
            {
                await ShowAlert("Ошибка", $"Не удалось сохранить заметку: {ex.Message}");
            }
        }

        private void NewNote(object sender, EventArgs e)
        {
            ClearEditor();
        }

        private void TogglePin(object sender, EventArgs e)
        {
            if (currentNote != null)
            {
                currentNote.IsPinned = !currentNote.IsPinned;
                SaveNote(sender, e);
            }
        }

        private async void AddTag(object sender, EventArgs e)
        {
            var tag = await DisplayPromptAsync("Добавить тег", "Введите тег:", "Добавить", "Отмена");
            if (!string.IsNullOrWhiteSpace(tag))
            {
                CurrentTags.Add(tag.Trim());
            }
        }

        private async void ShowColorPicker(object sender, EventArgs e)
        {
            var action = await DisplayActionSheet("Выберите цвет", "Отмена", null,
                "🔵 Синий", "🟢 Зеленый", "🟣 Фиолетовый", "🌸 Розовый", "🟡 Желтый", "⚫ Серый");

            if (currentNote != null && action != "Отмена")
            {
                currentNote.Color = action switch
                {
                    "🔵 Синий" => "Blue",
                    "🟢 Зеленый" => "Green",
                    "🟣 Фиолетовый" => "Purple",
                    "🌸 Розовый" => "Pink",
                    "🟡 Желтый" => "Yellow",
                    "⚫ Серый" => "Gray",
                    _ => "Default"
                };
            }
        }

        // ===== УДАЛЕНИЕ ЗАМЕТОК =====

        private async void DeleteSelectedNote(object sender, EventArgs e)
        {
            if (selectedNote != null && selectedNote.Id != 0)
            {
                bool confirm = await ShowConfirmationAlert("Удаление",
                    $"Вы уверены, что хотите удалить заметку \"{selectedNote.Title}\"?");

                if (confirm)
                {
                    try
                    {
                        var result = await _database.DeleteNoteAsync(selectedNote);

                        if (result > 0)
                        {
                            await LoadNotes();
                            selectedNote = null;
                            if (DeleteButton != null)
                                DeleteButton.IsVisible = false;
                            ClearEditor();
                            await ShowAlert("Успех", "Заметка удалена!");
                        }
                        else
                        {
                            await ShowAlert("Ошибка", "Не удалось удалить заметку");
                        }
                    }
                    catch (Exception ex)
                    {
                        await ShowAlert("Ошибка", $"Не удалось удалить заметку: {ex.Message}");
                    }
                }
            }
        }

        // ===== УНИКАЛЬНЫЕ ФУНКЦИИ ДЛЯ ЗАМЕТОК =====

        // РАБОЧИЙ ЭКСПОРТ ЗАМЕТОК
        private async void ExportNotes(object sender, EventArgs e)
        {
            try
            {
                if (!allNotes.Any())
                {
                    await ShowAlert("Экспорт", "Нет заметок для экспорта");
                    return;
                }

                // Создаем текстовый файл с заметками
                var exportLines = new List<string>();
                exportLines.Add("=== ЭКСПОРТ ЗАМЕТОК ===");
                exportLines.Add($"Дата экспорта: {DateTime.Now:dd.MM.yyyy HH:mm}");
                exportLines.Add($"Всего заметок: {allNotes.Count}");
                exportLines.Add("");

                foreach (var note in allNotes)
                {
                    exportLines.Add($"=== {note.Title} ===");
                    exportLines.Add($"Создано: {note.CreatedAt:dd.MM.yyyy HH:mm}");
                    exportLines.Add($"Изменено: {note.UpdateAt:dd.MM.yyyy HH:mm}");
                    if (!string.IsNullOrEmpty(note.TagsString))
                        exportLines.Add($"Теги: {note.TagsString.Replace(';', ',')}");
                    if (note.IsPinned)
                        exportLines.Add("📌 Закреплена");
                    if (note.HasTaskTag)
                        exportLines.Add("✅ Задача");
                    exportLines.Add("");
                    exportLines.Add(note.Content);
                    exportLines.Add("");
                    exportLines.Add(new string('-', 40));
                    exportLines.Add("");
                }

                var exportText = string.Join("\n", exportLines);

                // Сохраняем в файл
                var fileName = $"notes_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                await File.WriteAllTextAsync(filePath, exportText);

                // Показываем диалог успеха
                await ShowAlert("Экспорт завершен",
                    $"Успешно экспортировано {allNotes.Count} заметок\n\n" +
                    $"Файл: {fileName}\n" +
                    $"Вы можете найти его в кэше приложения");

            }
            catch (Exception ex)
            {
                await ShowAlert("Ошибка экспорта", $"Не удалось экспортировать заметки: {ex.Message}");
            }
        }

        private void SortAlphabetically(object sender, EventArgs e)
        {
            allNotes = allNotes.OrderBy(n => n.Title).ToList();
            CalculatePagination();
            RenderNotes();
        }

        private async void CreateQuickNote(object sender, EventArgs e)
        {
            var templates = new[]
            {
                "Идея: \nПроблема: \nРешение: ",
                "Встреча: \nУчастники: \nПовестка: \nИтоги: ",
                "Задача: \nШаги: \nДедлайн: ",
                "Конспект: \nКлючевые мысли: \nВыводы: "
            };

            var action = await DisplayActionSheet("Шаблон заметки", "Отмена", null,
                "💡 Идея", "📅 Встреча", "✅ Задача", "📚 Конспект");

            if (action != "Отмена")
            {
                var template = action switch
                {
                    "💡 Идея" => templates[0],
                    "📅 Встреча" => templates[1],
                    "✅ Задача" => templates[2],
                    "📚 Конспект" => templates[3],
                    _ => ""
                };

                if (ContentEditor != null)
                    ContentEditor.Text = template;
                if (TitleEntry != null)
                    TitleEntry.Text = action.Replace(" ", "");
                if (ContentEditor != null)
                    ContentEditor.Focus();
            }
        }

        private async void SearchByTag(object sender, EventArgs e)
        {
            var allTags = allNotes
                .SelectMany(n => n.TagsString.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .Distinct()
                .ToArray();

            if (!allTags.Any())
            {
                await ShowAlert("Теги", "Тегов не найдено");
                return;
            }

            var tag = await DisplayActionSheet("Поиск по тегу", "Отмена", null, allTags);

            if (tag != "Отмена")
            {
                var filtered = allNotes.Where(n => n.TagsString.Contains(tag)).ToList();
                allNotes = filtered;
                CalculatePagination();
                RenderNotes();
            }
        }

        // ===== ПОИСК И ФИЛЬТРАЦИЯ =====

        private async void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            var query = e.NewTextValue?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                await LoadNotes();
            }
            else
            {
                // ИСПРАВЛЕННЫЙ ВЫЗОВ С profileId
                var filtered = await _database.SearchNotesAsync(query, _currentProfileId);
                allNotes = filtered.OrderByDescending(n => n.IsPinned)
                                  .ThenByDescending(n => n.UpdatedAt)
                                  .ToList();
                CalculatePagination();
                RenderNotes();
            }
        }

        private void FilterAll(object sender, EventArgs e)
        {
            _ = LoadNotes();
        }

        private void FilterPinned(object sender, EventArgs e)
        {
            allNotes = allNotes.Where(n => n.IsPinned).ToList();
            CalculatePagination();
            RenderNotes();
        }

        private void FilterRecent(object sender, EventArgs e)
        {
            allNotes = allNotes.OrderByDescending(n => n.UpdateAt).Take(10).ToList();
            CalculatePagination();
            RenderNotes();
        }

        public void UpdateProfileStats()
        {
            try
            {
                int totalNotes = allNotes.Count;

                if (Application.Current?.MainPage is MainPage mainPage)
                {
                    mainPage.UpdateProfileStatistics(0, totalNotes, TimeSpan.Zero);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile stats from NotesView: {ex.Message}");
            }
        }

        // ===== ПАГИНАЦИЯ =====

        private void GoToFirstPage(object sender, EventArgs e)
        {
            CurrentPage = 1;
        }

        private void GoToPreviousPage(object sender, EventArgs e)
        {
            if (CurrentPage > 1)
                CurrentPage--;
        }

        private void GoToNextPage(object sender, EventArgs e)
        {
            if (CurrentPage < totalPages)
                CurrentPage++;
        }

        private void GoToLastPage(object sender, EventArgs e)
        {
            CurrentPage = totalPages;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}