using Windows.UI.Xaml;

namespace FsXaml
{
    public interface IEventArgsConverter
    {
        object Convert(RoutedEventArgs e, object parameter);
    }
}

