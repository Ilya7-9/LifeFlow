using Microsoft.Maui.Controls;
using Mauixui.Models;
using Mauixui.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace Mauixui.Views
{
    public partial class TasksView : ContentView
    {
        private readonly TaskDatabase _db;
        private readonly string _profileId;
        private readonly List<TaskItem> _tasks = new();
        private readonly List<TaskItem> _displayed = new();
        private readonly List<string> _categories = new() { "Общие", "Работа", "Учёба", "Личное" };
        private readonly List<string> _priorities = new() { "Низкий", "Средний", "Высокий", "Без приоритета" };

        private List<Subtask> _modalSubtasks = new();
        private TaskItem _editingTask = null;

        private enum DateFilter { All, Today, Tomorrow, Overdue }
        private DateFilter _dateFilter = DateFilter.All;
        private string _categoryFilter = null;
        private enum SortMode { DateDesc, DateAsc, TitleAsc, TitleDesc }
        private SortMode _sortMode = SortMode.DateDesc;

        private bool _isLoading = false;

        public TasksView()
        {
            InitializeComponent();

            var ps = new ProfileService();
            var cur = ps.GetCurrentProfile();
            _profileId = cur.Id;
            _db = ps.GetTaskDatabase(_profileId);

            BtnQuickActions.Clicked += BtnQuickActions_Clicked;
            _ = LoadTasksAsync();
        }

        private async Task LoadTasksAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                _tasks.Clear();
                _displayed.Clear();

                var loaded = await _db.GetTasksAsync(_profileId);

                foreach (var t in loaded)
                    t.Subtasks = await _db.GetUniqueSubtasksAsync(t.Id.ToString());

                var unique = loaded
                    .GroupBy(x => (x.Title ?? "").Trim().ToLower())
                    .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();

                _tasks.AddRange(unique);

                foreach (var c in _tasks.Select(t => t.Category).Where(c => !string.IsNullOrEmpty(c)))
                    if (!_categories.Contains(c)) _categories.Add(c);

                ModalCategoryPicker.ItemsSource = _categories;
                ModalPriorityPicker.ItemsSource = _priorities;

                ApplyFiltersAndRender();
                UpdateProfileStats();
            }
            catch (Exception ex)
            {
                await ShowAlert("Ошибка", "Не удалось загрузить задачи: " + ex.Message);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void SearchEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndRender();
        }

        private async void BtnQuickActions_Clicked(object sender, EventArgs e)
        {
            string action = await Application.Current.MainPage.DisplayActionSheet("Фильтры и сортировка",
                "Отмена", null,
                "Фильтр по дате",
                "Фильтр по категории",
                "Сортировка",
                "Сбросить фильтры");

            if (action == "Фильтр по дате")
            {
                string dateChoice = await Application.Current.MainPage.DisplayActionSheet("Дата", "Отмена", null,
                    "Все", "Сегодня", "Завтра", "Просрочено");
                if (dateChoice == "Все") _dateFilter = DateFilter.All;
                else if (dateChoice == "Сегодня") _dateFilter = DateFilter.Today;
                else if (dateChoice == "Завтра") _dateFilter = DateFilter.Tomorrow;
                else if (dateChoice == "Просрочено") _dateFilter = DateFilter.Overdue;

                ApplyFiltersAndRender();
            }
            else if (action == "Фильтр по категории")
            {
                var opts = _categories.ToArray();
                var cat = await Application.Current.MainPage.DisplayActionSheet("Категория", "Отмена", null, opts);
                if (!string.IsNullOrEmpty(cat) && cat != "Отмена")
                {
                    _categoryFilter = cat;
                    ApplyFiltersAndRender();
                }
            }
            else if (action == "Сортировка")
            {
                var sortChoice = await Application.Current.MainPage.DisplayActionSheet("Сортировка", "Отмена", null,
                    "По дате ↓ (новые сверху)", "По дате ↑ (старые сверху)", "По названию ↑", "По названию ↓");
                if (sortChoice == "По дате ↓ (новые сверху)") _sortMode = SortMode.DateDesc;
                else if (sortChoice == "По дате ↑ (старые сверху)") _sortMode = SortMode.DateAsc;
                else if (sortChoice == "По названию ↑") _sortMode = SortMode.TitleAsc;
                else if (sortChoice == "По названию ↓") _sortMode = SortMode.TitleDesc;

                ApplyFiltersAndRender();
            }
            else if (action == "Сбросить фильтры")
            {
                _dateFilter = DateFilter.All;
                _categoryFilter = null;
                _sortMode = SortMode.DateDesc;
                SearchEntry.Text = string.Empty;
                ApplyFiltersAndRender();
            }
        }

        private void BtnAddTask_Clicked(object sender, EventArgs e) => OpenCreateModal();

        private void OpenCreateModal()
        {
            _editingTask = null;
            ModalTitle.Text = "Создать задачу";
            ModalTitleEntry.Text = string.Empty;
            ModalDescriptionEditor.Text = string.Empty;
            ModalDeadlinePicker.Date = DateTime.Today;
            _modalSubtasks = new List<Subtask>();

            ModalCategoryPicker.ItemsSource = _categories;
            ModalPriorityPicker.ItemsSource = _priorities;
            ModalPriorityPicker.SelectedItem = "Средний";

            RenderModalSubtasks();
            ModalOverlay.IsVisible = true;
        }

        private void ModalAddCategoryBtn_Clicked(object sender, EventArgs e)
        {
            var newCat = ModalNewCategoryEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newCat)) return;
            if (!_categories.Contains(newCat))
            {
                _categories.Add(newCat);
                ModalCategoryPicker.ItemsSource = null;
                ModalCategoryPicker.ItemsSource = _categories;
            }
            ModalCategoryPicker.SelectedItem = newCat;
            ModalNewCategoryEntry.Text = string.Empty;
        }

        private void ModalAddSubtaskBtn_Clicked(object sender, EventArgs e)
        {
            if (_modalSubtasks.Count >= 7)
            {
                _ = ShowAlert("Ограничение", "Можно добавить максимум 7 подзадач.");
                return;
            }

            var text = ModalSubtaskEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;
            var sub = new Subtask { Title = text, IsCompleted = false, CreatedAt = DateTime.Now };
            _modalSubtasks.Add(sub);
            ModalSubtaskEntry.Text = string.Empty;
            RenderModalSubtasks();
        }

        private void RenderModalSubtasks()
        {
            ModalSubtasksLayout.Children.Clear();
            foreach (var s in _modalSubtasks.ToList())
            {
                var row = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
                var lbl = new Label { Text = s.Title, VerticalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#DDDDDD") };
                var del = new Button { Text = "🗑", WidthRequest = 36, HeightRequest = 36, BackgroundColor = new Color(0, 0, 0, 0), TextColor = Color.FromArgb("#FF6B6B") };
                del.Clicked += (sender, e) =>
                {
                    _modalSubtasks.Remove(s);
                    RenderModalSubtasks();
                };
                row.Add(lbl);
                row.Add(del);
                ModalSubtasksLayout.Add(row);
            }
        }

        private void ModalCancel_Clicked(object sender, EventArgs e) => ModalOverlay.IsVisible = false;

        private async void ModalSaveBtn_Clicked(object sender, EventArgs e)
        {
            ModalSaveBtn.IsEnabled = false;
            try
            {
                string title = ModalTitleEntry.Text?.Trim();
                string desc = ModalDescriptionEditor.Text?.Trim();
                var cat = ModalCategoryPicker.SelectedItem as string ?? "Общие";
                var priority = ModalPriorityPicker.SelectedItem as string ?? "Средний";
                DateTime? deadline = ModalDeadlinePicker.Date;

                if (string.IsNullOrWhiteSpace(title))
                {
                    await ShowAlert("Ошибка", "Название задачи не может быть пустым.");
                    return;
                }

                if (_editingTask != null)
                {
                    _editingTask.Title = title;
                    _editingTask.Description = desc;
                    _editingTask.Category = cat;
                    _editingTask.Priority = priority;
                    _editingTask.Deadline = deadline;
                    await _db.SaveTaskAsync(_editingTask);
                    await _db.DeleteAllSubtasksAsync(_editingTask.Id.ToString());
                    await SaveSubtasksToDatabase(_editingTask.Id.ToString());
                }
                else
                {
                    var newTask = new TaskItem
                    {
                        ProfileId = _profileId,
                        Title = title,
                        Description = desc,
                        CreatedAt = DateTime.Now,
                        Deadline = deadline,
                        Category = cat,
                        Priority = priority,
                        IsCompleted = false,
                        IsFavorite = false
                    };
                    await _db.SaveTaskAsync(newTask);
                    _tasks.Insert(0, newTask);
                    await SaveSubtasksToDatabase(newTask.Id.ToString());
                }

                if (!_categories.Contains(cat)) _categories.Add(cat);

                ModalOverlay.IsVisible = false;
                _modalSubtasks = new List<Subtask>();
                ApplyFiltersAndRender();
                UpdateProfileStats();
            }
            catch (Exception ex)
            {
                await ShowAlert("Ошибка", "Не удалось сохранить задачу: " + ex.Message);
            }
            finally
            {
                ModalSaveBtn.IsEnabled = true;
            }
        }

        private async Task SaveSubtasksToDatabase(string taskItemId)
        {
            foreach (var subtask in _modalSubtasks)
            {
                subtask.TaskItemId = taskItemId; // Привязываем к родительской задаче
                subtask.CreatedAt = DateTime.Now;
                await _db.SaveSubtaskAsync(subtask);
            }
        }

        private void ApplyFiltersAndRender()
        {
            var q = _tasks.AsEnumerable();

            var search = SearchEntry.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                q = q.Where(t => (t.Title ?? "").ToLower().Contains(search) || (t.Description ?? "").ToLower().Contains(search));
            }

            var now = DateTime.Today;
            if (_dateFilter == DateFilter.Today) q = q.Where(t => t.Deadline.HasValue && t.Deadline.Value.Date == now);
            else if (_dateFilter == DateFilter.Tomorrow) q = q.Where(t => t.Deadline.HasValue && t.Deadline.Value.Date == now.AddDays(1));
            else if (_dateFilter == DateFilter.Overdue) q = q.Where(t => t.Deadline.HasValue && t.Deadline.Value.Date < now && !t.IsCompleted);

            if (!string.IsNullOrEmpty(_categoryFilter)) q = q.Where(t => (t.Category ?? "") == _categoryFilter);

            switch (_sortMode)
            {
                case SortMode.DateDesc: q = q.OrderByDescending(t => t.CreatedAt); break;
                case SortMode.DateAsc: q = q.OrderBy(t => t.CreatedAt); break;
                case SortMode.TitleAsc: q = q.OrderBy(t => t.Title); break;
                case SortMode.TitleDesc: q = q.OrderByDescending(t => t.Title); break;
            }

            _displayed.Clear();
            _displayed.AddRange(q);
            RenderTasks(_displayed);
        }

        private void RenderTasks(IEnumerable<TaskItem> list)
        {
            TasksList.Children.Clear();
            var items = (list ?? _displayed).ToList();
            if (!items.Any())
            {
                TasksList.Children.Add(new Label
                {
                    Text = "📋\nЗадач пока нет.\nНажмите ➕ чтобы создать задачу.",
                    HorizontalTextAlignment = TextAlignment.Center,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#CCCCCC"),
                    Margin = new Thickness(0, 16)
                });
                return;
            }

            foreach (var t in items) TasksList.Children.Add(CreateTaskFrame(t));
        }

        private Frame CreateTaskFrame(TaskItem task)
        {
            var bg = task.IsCompleted ? Color.FromArgb("#2D2D30") : Color.FromArgb("#40444B");

            var frame = new Frame
            {
                CornerRadius = 12,
                Padding = 12,
                BackgroundColor = bg,
                HasShadow = true,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var root = new VerticalStackLayout { Spacing = 8 };

            // 🔴 Кружок приоритета
            Color priorityColor = task.Priority switch
            {
                "Низкий" => Color.FromArgb("#4CAF50"),
                "Средний" => Color.FromArgb("#FFC107"),
                "Высокий" => Color.FromArgb("#F44336"),
                _ => Color.FromArgb("#888888")
            };

            var priorityDot = new BoxView
            {
                WidthRequest = 24,
                HeightRequest = 24,
                CornerRadius = 12,
                Color = priorityColor,
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 15, 0)
            };

            var titleRow = new Grid
            {
                ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                VerticalOptions = LayoutOptions.Center
            }; 

            var titleLabel = new Label
            {
                Text = task.Title,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                TextColor = task.IsCompleted ? Color.FromArgb("#888888") : Color.FromArgb("#FFFFFF")
            };

            var menuBtn = new Button
            {

                Text = "⋮",
                WidthRequest = 44,
                HeightRequest = 44,
                CornerRadius = 10,
                BackgroundColor = new Color(0, 0, 0, 0),
                TextColor = Color.FromArgb("#CCCCCC"),
                HorizontalOptions = LayoutOptions.End
            };

            menuBtn.Clicked += async (s, e) =>
            {
                string action = await Application.Current.MainPage.DisplayActionSheet(
                    "Управление задачей",
                    "Отмена",
                    null,
                    "Редактировать",
                    task.IsCompleted ? "Отметить невыполненной" : "Отметить выполненной",
                    "Удалить"
                );

                if (action == "Редактировать") OpenEditModal(task);
                else if (action == "Удалить")
                {
                    bool ok = await ShowConfirmationAlert("Удаление", $"Удалить задачу \"{task.Title}\"?");
                    if (!ok) return;
                    await _db.DeleteTaskAsync(task);
                    _tasks.Remove(task);
                    ApplyFiltersAndRender();
                }
                else if (action == "Отметить выполненной" || action == "Отметить невыполненной")
                {
                    task.IsCompleted = !task.IsCompleted;
                    await _db.SaveTaskAsync(task);
                    ApplyFiltersAndRender();
                }
            };

            titleRow.Add(titleLabel);
            Grid.SetColumn(titleLabel, 0);

            titleRow.Add(priorityDot);
            Grid.SetColumn(priorityDot, 1);

            titleRow.Add(menuBtn);
            Grid.SetColumn(menuBtn, 2);

            root.Add(titleRow);

            if (!string.IsNullOrWhiteSpace(task.Description))
                root.Add(new Label
                {
                    Text = task.Description,
                    FontSize = 13,
                    TextColor = Color.FromArgb("#CCCCCC"),
                    MaxLines = 3
                });

            var category = new Label
            {
                Text = $"Категория: {task.Category}",
                FontSize = 12,
                TextColor = Color.FromArgb("#AAAAAA")
            };

            var startTime = new Label
            {
                Text = $"Создан: {task.CreatedAt:dd.MM.yyyy}",
                FontSize = 12,
                HorizontalOptions = LayoutOptions.Start,
                TextColor = Color.FromArgb("#AAAAAA")
            };

            var deadline = new Label
            {
                Text = $"Дедлайн: {task.Deadline:dd.MM.yyyy}",
                FontSize = 12,
                HorizontalOptions = LayoutOptions.Start,
                TextColor = Color.FromArgb("#AAAAAA")
            };

            var BottomRow = new Grid
            {
                ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                RowDefinitions =
                    {
                        new RowDefinition {Height = GridLength.Auto },
                        new RowDefinition {Height = GridLength.Auto }
                    },
                VerticalOptions = LayoutOptions.Center
            };

            BottomRow.Add(startTime);
            Grid.SetColumn(startTime, 1);
            Grid.SetRow(startTime, 0);

            BottomRow.Add(deadline);
            Grid.SetColumn(deadline, 1);
            Grid.SetRow(deadline, 1);

            BottomRow.Add(category);
            Grid.SetColumn(category, 0);
            Grid.SetRow(category, 1);

            root.Add(BottomRow);

            frame.Content = root;

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                bool ok = await ShowConfirmationAlert("Выполнено?", $"Отметить задачу \"{task.Title}\" как выполненную и удалить?");
                if (!ok) return;
                await _db.DeleteTaskAsync(task);
                _tasks.Remove(task);
                ApplyFiltersAndRender();
            };
            frame.GestureRecognizers.Add(tap);

            if (task.Subtasks != null && task.Subtasks.Any())
            {
                var subtasksLabel = new Label
                {
                    Text = $"Подзадачи: {task.Subtasks.Count}",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#AAAAAA")
                };
                root.Add(subtasksLabel);

                // Можно добавить отображение списка подзадач
                foreach (var subtask in task.Subtasks)
                {
                    var subtaskLayout = new HorizontalStackLayout { Spacing = 8 };
                    var checkbox = new CheckBox { IsChecked = subtask.IsCompleted };
                    var subtaskLabel = new Label
                    {
                        Text = subtask.Title,
                        TextColor = subtask.IsCompleted ? Color.FromArgb("#888888") : Color.FromArgb("#CCCCCC"),
                        TextDecorations = subtask.IsCompleted ? TextDecorations.Strikethrough : TextDecorations.None
                    };

                    subtaskLayout.Add(checkbox);
                    subtaskLayout.Add(subtaskLabel);
                    root.Add(subtaskLayout);
                }
            }

            return frame;
        }

        private void OpenEditModal(TaskItem task)
        {
            _editingTask = task;
            ModalTitle.Text = "Редактировать задачу";
            ModalTitleEntry.Text = task.Title;
            ModalDescriptionEditor.Text = task.Description;
            ModalDeadlinePicker.Date = task.Deadline ?? DateTime.Today;

            if (!_categories.Contains(task.Category) && !string.IsNullOrEmpty(task.Category))
                _categories.Add(task.Category);

            ModalCategoryPicker.ItemsSource = _categories;
            ModalCategoryPicker.SelectedItem = task.Category;

            ModalPriorityPicker.ItemsSource = _priorities;
            ModalPriorityPicker.SelectedItem = task.Priority ?? "Средний";

            ModalOverlay.IsVisible = true;
        }

        private void UpdateProfileStats()
        {
            try
            {
                if (Application.Current?.MainPage is MainPage main)
                {
                    int total = _tasks.Count;
                    int done = _tasks.Count(t => t.IsCompleted);
                    main.UpdateProfileStatistics(total, done, TimeSpan.Zero);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateProfileStats error: " + ex.Message);
            }
        }

        private async Task ShowAlert(string title, string message)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }

        private async Task<bool> ShowConfirmationAlert(string title, string message)
        {
            if (Application.Current?.MainPage != null)
                return await Application.Current.MainPage.DisplayAlert(title, message, "Да", "Нет");
            return false;
        }
    }
}
