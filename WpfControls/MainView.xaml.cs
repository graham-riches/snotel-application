using System.Windows;
using System.Windows.Input;

namespace WpfControls
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            MainFrame.Navigate(new HomePage());
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseAppHandler(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NavigateHomeHandler(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HomePage());
        }

        private void NavigateSiteHandler(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SitePage());
        }
    }
}
