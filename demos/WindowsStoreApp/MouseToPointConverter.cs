using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using FsXaml;

namespace WindowsStoreApp
{
    internal class MouseToPointConverter : IEventArgsConverter
    {
        public object Convert(RoutedEventArgs ea, object param)
        {
            var args = ea as PointerRoutedEventArgs;
            var source = args.OriginalSource as FrameworkElement;

            var pt = args.GetCurrentPoint(source);
            return new ViewModels.Point(pt.Position.X, pt.Position.Y);
        }
    }
}