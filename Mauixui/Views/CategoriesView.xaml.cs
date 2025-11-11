using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mauixui.Services;
using Mauixui.Models;

namespace Mauixui.Views
{
    public partial class CategoriesView : ContentView
    {
        private List<CategoryItem> _categories = new();
        private CategoryDatabase _db;
        private string _profileId;

        public CategoriesView()
        {
            InitializeComponent();

            var profileService = new ProfileService();
            _profileId = profileService.GetCurrentProfile().Id;
            _db = profileService.GetCategoryDatabase(_profileId);

            LoadCategories();
        }

        private async void LoadCategories()
        {
            _categories = await _db.GetCategoriesAsync(_profileId);
            RenderCategories();
        }

        private async void AddCategory(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryEntry.Text) || TypePicker.SelectedIndex == -1)
                return;

            var item = new CategoryItem
            {
                ProfileId = _profileId,
                Name = CategoryEntry.Text,
                Type = TypePicker.SelectedItem.ToString()
            };

            await _db.SaveCategoryAsync(item);
            CategoryEntry.Text = "";
            TypePicker.SelectedIndex = -1;
            LoadCategories();
        }

        private void RenderCategories()
        {
            CategoryList.Children.Clear();
            if (!_categories.Any())
            {
                CategoryList.Children.Add(new Label
                {
                    Text = "Категорий нет",
                    TextColor = Color.FromArgb("#888888"),
                    HorizontalOptions = LayoutOptions.Center
                });
                return;
            }

            foreach (var cat in _categories)
            {
                var frame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#40444B"),
                    CornerRadius = 12,
                    Padding = 10,
                    Content = new Label
                    {
                        Text = $"{cat.Name} ({cat.Type})",
                        TextColor = Color.FromArgb("#FFFFFF"),
                        FontSize = 14
                    }
                };
                CategoryList.Children.Add(frame);
            }
        }
    }
}
