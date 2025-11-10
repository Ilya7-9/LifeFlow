using System.Globalization;

namespace Mauixui.Converters
{
    public class NoteColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = value as string;
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}