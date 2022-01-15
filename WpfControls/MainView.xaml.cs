using System.Windows;
using System.Windows.Input;

namespace WpfControls
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();            
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
            Application.Current.Shutdown();
        }
    }
}
