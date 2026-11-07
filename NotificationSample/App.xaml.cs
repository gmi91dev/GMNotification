using System.Windows;
using GMNotification;

namespace NotificationSample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            NotificationCenter.DefaultCenter().Dispose();
            base.OnExit(e);
        }
    }

}
