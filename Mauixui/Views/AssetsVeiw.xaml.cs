using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mauixui.Services;
using Mauixui.Models;

namespace Mauixui.Views
{
    public partial class AssetsView : ContentView
    {
        private List<Asset> _assets = new();
        private AssetDatabase _db;
        private string _profileId;

        public AssetsView()
        {
            InitializeComponent();

            var profileService = new ProfileService();
            _profileId = profileService.GetCurrentProfile().Id;
            _db = profileService.GetAssetDatabase(_profileId);

            LoadAssets();
        }

        private async void LoadAssets()
        {
            _assets = await _db.GetAssetsAsync(_profileId);
            RenderAssets();
        }

        private async void AddAsset(object sender, EventArgs e)
        {
            if (!decimal.TryParse(AssetValueEntry.Text, out decimal val)) return;

            var a = new Asset
            {
                ProfileId = _profileId,
                Name = AssetNameEntry.Text,
                Type = AssetTypePicker.SelectedItem?.ToString() ?? "Актив",
                Value = val
            };
            await _db.SaveAssetAsync(a);
            LoadAssets();
        }

        private void RenderAssets()
        {
            AssetsList.Children.Clear();

            decimal total = _assets.Sum(a => a.Type == "Актив" ? a.Value : -a.Value);

            foreach (var a in _assets)
            {
                var color = a.Type == "Актив" ? "#23D160" : "#FF4B4B";
                var label = new Label
                {
                    Text = $"{a.Name} — {a.Value:F2} ₽ ({a.Type})",
                    TextColor = Color.FromArgb(color)
                };
                AssetsList.Children.Add(label);
            }

            AssetsList.Children.Add(new Label
            {
                Text = $"\nЧистый капитал: {total:F2} ₽",
                FontAttributes = FontAttributes.Bold,
                TextColor = total >= 0 ? Color.FromArgb("#23D160") : Color.FromArgb("#FF4B4B")
            });
        }
    }
}
