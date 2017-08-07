using System.Windows;

namespace SampleApp
{
    public partial class AppLauncher
    {
        public AppLauncher()
        {
            InitializeComponent();
        }

        private void LaunchJustGestures(object sender, RoutedEventArgs e)
        {
            new JustGesturesWindow().Show();
        }

        private void LaunchGestureOnGesture(object sender, RoutedEventArgs e)
        {
            new GesturesOnGesturesWindow().Show();
        }
    }
}
