using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using Mauixui.Models;

namespace Mauixui.Services
{
    public class NoteDatabase
    {
        private SQLiteAsyncConnection _database;

        public NoteDatabase(string dbPath)
        {
            try
            {
                _database = new SQLiteAsyncConnection(dbPath);
                _database.CreateTableAsync<NoteItem>().Wait();
                Console.WriteLine($"База заметок инициализирована: {dbPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации базы данных: {ex.Message}");
            }
        }

        public async Task<List<NoteItem>> GetNotesAsync(string profileId)
        {
            try
            {
                if (string.IsNullOrEmpty(profileId))
                    return new List<NoteItem>();

                var notes = await _database.Table<NoteItem>()
                    .Where(n => n.ProfileId == profileId)
                    .ToListAsync();

                Console.WriteLine($"Загружено {notes?.Count ?? 0} заметок для профиля {profileId}");
                return notes ?? new List<NoteItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки заметок: {ex.Message}");
                return new List<NoteItem>();
            }
        }

        public Task<NoteItem> GetNoteAsync(int id, string profileId)
        {
            return _database.Table<NoteItem>()
                .Where(n => n.Id == id && n.ProfileId == profileId)
                .FirstOrDefaultAsync();
        }

        public async Task<int> SaveNoteAsync(NoteItem note)
        {
            try
            {
                if (note == null)
                    return 0;

                // Убедимся, что ProfileId установлен
                if (string.IsNullOrEmpty(note.ProfileId))
                {
                    Console.WriteLine("Ошибка: ProfileId не установлен");
                    return 0;
                }

                note.UpdatedAt = DateTime.Now;

                if (note.Id != 0)
                {
                    var result = await _database.UpdateAsync(note);
                    Console.WriteLine($"Заметка обновлена: {note.Id}, {note.Title}");
                    return result;
                }
                else
                {
                    note.CreatedAt = DateTime.Now;
                    var result = await _database.InsertAsync(note);
                    Console.WriteLine($"Заметка создана: {result}, {note.Title}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения заметки: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> DeleteNoteAsync(NoteItem note)
        {
            try
            {
                if (note?.Id == 0)
                    return 0;

                var result = await _database.DeleteAsync(note);
                Console.WriteLine($"Заметка удалена: {note.Id}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления заметки: {ex.Message}");
                return 0;
            }
        }

        public Task<List<NoteItem>> GetPinnedNotesAsync(string profileId)
        {
            return _database.Table<NoteItem>()
                .Where(n => n.IsPinned && n.ProfileId == profileId)
                .ToListAsync();
        }

        public Task<List<NoteItem>> GetNotesByCategoryAsync(string category, string profileId)
        {
            return _database.Table<NoteItem>()
                .Where(n => n.Category == category && n.ProfileId == profileId)
                .ToListAsync();
        }

        public Task<List<NoteItem>> SearchNotesAsync(string query, string profileId)
        {
            if (string.IsNullOrEmpty(query))
                return GetNotesAsync(profileId);

            return _database.Table<NoteItem>()
                .Where(n => n.ProfileId == profileId &&
                           (n.Title.Contains(query) || n.Content.Contains(query) || n.TagsString.Contains(query)))
                .ToListAsync();
        }

        public async Task<int> GetNotesCountAsync(string profileId)
        {
            var notes = await GetNotesAsync(profileId);
            return notes.Count;
        }
    }
}