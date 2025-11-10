using SQLite;
using Mauixui.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Mauixui.Services
{
    public class TaskDatabase
    {
        private SQLiteAsyncConnection database;
        private string _dbPath;
        private bool isInitialized = false;

        public TaskDatabase(string dbPath)
        {
            _dbPath = dbPath;


            database = new SQLiteAsyncConnection(dbPath);
            _dbPath = dbPath;

            Console.WriteLine($"🔄 Создаем новую БД: {Path.GetFileName(dbPath)}");
            // НЕМЕДЛЕННО создаем таблицы
            //CreateTablesSync();
        }

        private void CreateTablesSync()
        {
            try
            {
                // СОЗДАЕМ ТАБЛИЦЫ С PRIMARY KEYS
                database.CreateTableAsync<TaskItem>().Wait();
                database.CreateTableAsync<Subtask>().Wait();
                Console.WriteLine("✅ Таблицы созданы с Primary Keys");

                isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка создания таблиц: {ex.Message}");
                throw;
            }
        }

        private async Task InitializeAsync()
        {
            if (!isInitialized)
            {
                await database.CreateTableAsync<TaskItem>();
                await database.CreateTableAsync<Subtask>();
                isInitialized = true;
                Console.WriteLine("✅ Таблицы инициализированы");
            }
        }

        // ===== МЕТОДЫ ДЛЯ TASKITEM =====

        public async Task<List<TaskItem>> GetTasksAsync()
        {
            await InitializeAsync();
            return await database.Table<TaskItem>().ToListAsync();
        }

        public async Task<List<TaskItem>> GetTasksAsync(string profileId)
        {
            await InitializeAsync();
            return await database.Table<TaskItem>()
                .Where(t => t.ProfileId == profileId)
                .ToListAsync();
        }

        public async Task<TaskItem> GetTaskAsync(int id)
        {
            await InitializeAsync();
            return await database.Table<TaskItem>()
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<TaskItem> GetTaskAsync(string profileId, int id)
        {
            await InitializeAsync();
            return await database.Table<TaskItem>()
                .Where(t => t.ProfileId == profileId && t.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> SaveTaskAsync(TaskItem task)
        {
            await InitializeAsync();

            if (task.Id == 0)
                return await database.InsertAsync(task);  // Новая запись
            else
                return await database.UpdateAsync(task);  // Обновление существующей
        }

        public async Task<int> DeleteTaskAsync(TaskItem task)
        {
            await InitializeAsync();
            return await database.DeleteAsync(task);
        }

        // ===== МЕТОДЫ ДЛЯ SUBTASK =====

        public async Task<List<Subtask>> GetSubtasksAsync(string taskItemId)
        {
            await InitializeAsync();
            return await database.Table<Subtask>()
                .Where(s => s.TaskItemId == taskItemId)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Subtask> GetSubtaskAsync(string id)
        {
            await InitializeAsync();
            return await database.Table<Subtask>()
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> SaveSubtaskAsync(Subtask subtask)
        {
            await InitializeAsync();
            return await database.InsertOrReplaceAsync(subtask);
        }

        public async Task<int> DeleteSubtaskAsync(Subtask subtask)
        {
            await InitializeAsync();
            return await database.DeleteAsync(subtask);
        }

        public async Task<int> DeleteAllSubtasksAsync(string taskItemId)
        {
            await InitializeAsync();
            return await database.Table<Subtask>()
                .Where(s => s.TaskItemId == taskItemId)
                .DeleteAsync();
        }

        // ===== СИЛЬНЫЕ МЕТОДЫ ДЛЯ ОБХОДА ОШИБОК PK =====

        public async Task ForceDeleteTaskAsync(TaskItem task)
        {
            await InitializeAsync();

            try
            {
                // Сначала пробуем стандартный метод
                var result = await database.DeleteAsync(task);
                Console.WriteLine($"✅ Задача {task.Id} '{task.Title}' удалена стандартным методом");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Стандартное удаление не удалось: {ex.Message}");
                Console.WriteLine($"🔄 Пробуем удалить через SQL...");

                // Используем прямой SQL
                if (task.Id > 0)
                {
                    await database.ExecuteAsync("DELETE FROM TaskItem WHERE Id = ?", task.Id);
                    Console.WriteLine($"✅ Задача {task.Id} удалена через SQL");
                }
                else
                {
                    // Удаляем по другим полям
                    await database.ExecuteAsync(
                        "DELETE FROM TaskItem WHERE Title = ? AND CreatedAt = ? AND ProfileId = ?",
                        task.Title, task.CreatedAt, task.ProfileId);
                    Console.WriteLine($"✅ Задача '{task.Title}' удалена по составному ключу");
                }
            }
        }

        public async Task ForceDeleteSubtaskAsync(Subtask subtask)
        {
            await InitializeAsync();

            try
            {
                var result = await database.DeleteAsync(subtask);
                Console.WriteLine($"✅ Подзадача {subtask.Id} удалена стандартным методом");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Стандартное удаление подзадачи не удалось: {ex.Message}");
                Console.WriteLine($"🔄 Пробуем удалить через SQL...");

                await database.ExecuteAsync("DELETE FROM Subtask WHERE Id = ?", subtask.Id);
                Console.WriteLine($"✅ Подзадача {subtask.Id} удалена через SQL");
            }
        }

        // ===== МЕТОДЫ ДЛЯ МАССОВЫХ ОПЕРАЦИЙ =====

        public async Task<int> SaveTasksAsync(IEnumerable<TaskItem> tasks)
        {
            await InitializeAsync();
            return await database.InsertAllAsync(tasks);
        }

        public async Task<int> SaveSubtasksAsync(IEnumerable<Subtask> subtasks)
        {
            await InitializeAsync();
            return await database.InsertAllAsync(subtasks);
        }

        // ===== ДИАГНОСТИЧЕСКИЕ МЕТОДЫ =====

        public async Task DebugTableStructure()
        {
            await InitializeAsync();

            try
            {
                // Проверяем структуру таблицы TaskItem
                var tableInfo = await database.QueryAsync<TableInfo>("PRAGMA table_info(TaskItem)");
                Console.WriteLine("=== СТРУКТУРА ТАБЛИЦЫ TaskItem ===");
                foreach (var column in tableInfo)
                {
                    Console.WriteLine($"Столбец: {column.name}, Тип: {column.type}, PK: {column.pk}");
                }

                // Проверяем структуру таблицы Subtask
                var subtaskTableInfo = await database.QueryAsync<TableInfo>("PRAGMA table_info(Subtask)");
                Console.WriteLine("=== СТРУКТУРА ТАБЛИЦЫ Subtask ===");
                foreach (var column in subtaskTableInfo)
                {
                    Console.WriteLine($"Столбец: {column.name}, Тип: {column.type}, PK: {column.pk}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка диагностики: {ex.Message}");
            }
        }

        public async Task<int> GetTasksCountAsync(string profileId = null)
        {
            await InitializeAsync();
            if (string.IsNullOrEmpty(profileId))
            {
                return await database.Table<TaskItem>().CountAsync();
            }
            else
            {
                return await database.Table<TaskItem>()
                    .Where(t => t.ProfileId == profileId)
                    .CountAsync();
            }
        }

        public async Task<int> GetSubtasksCountAsync(string taskItemId = null)
        {
            await InitializeAsync();
            if (string.IsNullOrEmpty(taskItemId))
            {
                return await database.Table<Subtask>().CountAsync();
            }
            else
            {
                return await database.Table<Subtask>()
                    .Where(s => s.TaskItemId == taskItemId)
                    .CountAsync();
            }
        }

        // ===== МЕТОДЫ ДЛЯ ОЧИСТКИ =====

        public async Task ClearAllDataAsync()
        {
            await InitializeAsync();
            await database.DeleteAllAsync<TaskItem>();
            await database.DeleteAllAsync<Subtask>();
            Console.WriteLine("✅ Все данные очищены");
        }

        public async Task RecreateTablesAsync()
        {
            await InitializeAsync();
            await database.DropTableAsync<TaskItem>();
            await database.DropTableAsync<Subtask>();
            await database.CreateTableAsync<TaskItem>();
            await database.CreateTableAsync<Subtask>();
            Console.WriteLine("✅ Таблицы пересозданы");
        }

        // ===== ВСПОМОГАТЕЛЬНЫЙ КЛАСС ДЛЯ ДИАГНОСТИКИ =====

        public class TableInfo
        {
            public string name { get; set; }
            public string type { get; set; }
            public int pk { get; set; }
        }

        // В TaskDatabase.cs добавьте метод
        public async Task<bool> TaskExistsAsync(string profileId, string title)
        {
            // Упрощенная проверка без DateTime.Date
            return await database.Table<TaskItem>()
                .Where(t => t.ProfileId == profileId && t.Title == title)
                .CountAsync() > 0;
        }

        public async Task<bool> SubtaskExistsAsync(string taskItemId, string title)
        {
            // Упрощенная проверка без DateTime.Date
            return await database.Table<Subtask>()
                .Where(s => s.TaskItemId == taskItemId && s.Title == title)
                .CountAsync() > 0;
        }

        public async Task<List<Subtask>> GetUniqueSubtasksAsync(string taskItemId)
        {
            var allSubtasks = await database.Table<Subtask>()
                .Where(s => s.TaskItemId == taskItemId)
                .ToListAsync();

            // Удаляем дубликаты в памяти (после загрузки)
            var uniqueSubtasks = allSubtasks
                .GroupBy(s => s.Title) // Только по названию
                .Select(g => g.OrderByDescending(s => s.CreatedAt).First())
                .ToList();

            return uniqueSubtasks;
        }

        public async Task CleanupDuplicateSubtasksAsync(string taskItemId)
        {
            var allSubtasks = await database.Table<Subtask>()
                .Where(s => s.TaskItemId == taskItemId)
                .ToListAsync();

            // Очищаем дубликаты в памяти
            var duplicates = allSubtasks
                .GroupBy(s => s.Title)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.OrderByDescending(s => s.CreatedAt).Skip(1))
                .ToList();

            foreach (var duplicate in duplicates)
            {
                await database.DeleteAsync(duplicate);
            }
        }

        // Новый метод для очистки дубликатов задач
        public async Task CleanupDuplicateTasksAsync(string profileId)
        {
            var allTasks = await database.Table<TaskItem>()
                .Where(t => t.ProfileId == profileId)
                .ToListAsync();

            var duplicates = allTasks
                .GroupBy(t => t.Title)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.OrderByDescending(t => t.CreatedAt).Skip(1))
                .ToList();

            foreach (var duplicate in duplicates)
            {
                // Сначала удаляем подзадачи
                var subtasks = await database.Table<Subtask>()
                    .Where(s => s.TaskItemId == duplicate.Id.ToString())
                    .ToListAsync();

                foreach (var subtask in subtasks)
                {
                    await database.DeleteAsync(subtask);
                }

                // Затем удаляем задачу
                await database.DeleteAsync(duplicate);
            }
        }

    }
}