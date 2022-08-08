namespace ARToolkit.Samples.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            var np = new NavigationPage();
            MainPage = np;
            np.Navigation.PushAsync(new MainPage());
        }
    }
}