using Avalonia.Controls;
using Avalonia.Input;
using mbclientava.ViewModels;

namespace mbclientava.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            HideButton.Click += (s, e) => WindowState = WindowState.Minimized;
            CloseButton.Click += (s, e) => Close();
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Запускаем нативное перетаскивание
            BeginMoveDrag(e);
        }
    }
}
 