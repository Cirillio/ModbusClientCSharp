namespace mbclientava.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string Greeting { get; } = "Welcome to Avalonia,";
        public string Name { get; set; } = "";
    }
}
