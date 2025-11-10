using System;
using System.ComponentModel;

namespace Mauixui.Models
{
    public class NoteItem : INotifyPropertyChanged
    {
        // ИЗМЕНЕНО: int вместо string
        public int Id { get; set; }

        public string ProfileId { get; set; }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        private string _content;
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(Preview));
            }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        private DateTime _updatedAt = DateTime.Now;
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set
            {
                _updatedAt = value;
                OnPropertyChanged(nameof(UpdatedAt));
                OnPropertyChanged(nameof(UpdateAt)); // Уведомляем оба свойства
            }
        }

        // Синхронизируем UpdatedAt и UpdateAt
        public DateTime UpdateAt
        {
            get => UpdatedAt;
            set => UpdatedAt = value;
        }

        private string _color = "Default";
        public string Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged(nameof(Color));
            }
        }

        private bool _isPinned;
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                _isPinned = value;
                OnPropertyChanged(nameof(IsPinned));
            }
        }

        public string Category { get; set; } = "Общие";

        private string _tagsString = "";
        public string TagsString
        {
            get => _tagsString;
            set
            {
                _tagsString = value;
                OnPropertyChanged(nameof(TagsString));
                OnPropertyChanged(nameof(HasTaskTag));
            }
        }

        private bool _isConvertedToTask;
        public bool IsConvertedToTask
        {
            get => _isConvertedToTask;
            set
            {
                _isConvertedToTask = value;
                OnPropertyChanged(nameof(IsConvertedToTask));
            }
        }

        // УДАЛЕНО: NoteId - это дублирование Id

        public bool HasTaskTag => !string.IsNullOrEmpty(TagsString) && TagsString.Contains("#задача");

        public string Preview => Content?.Length > 100 ? Content.Substring(0, 100) + "..." : Content ?? "";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}