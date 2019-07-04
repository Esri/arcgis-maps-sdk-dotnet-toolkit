using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace ARToolkit.SampleApp.Forms
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

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
