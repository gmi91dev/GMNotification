using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GMNotification;

namespace NotificationSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            NotificationCenter.DefaultCenter().AddObserver(this, "resuest.start", noti =>
            {
                Update("获取数据中...");
                
            });

            NotificationCenter.DefaultCenter().AddObserver(this, "resuest.success", ResuestSuccess);

            NotificationCenter.DefaultCenter().AddObserver(this, "resuest2.success", ResuestSuccess);

            var manager = new HTTPManager();

            NotificationCenter.DefaultCenter().RemoveObserver(this);

            manager.Request();
        }

        private void ResuestSuccess(Notification noti)
        {
            //NotificationCenter.DefaultCenter().RemoveObserver(this, "resuest2.success");

            Dictionary<string, string> dic = noti.UserInfo as Dictionary<string, string>;

            if (dic != null)
            {
                var t = $"{noti.Name}: {dic["name"]}， {dic["age"]}";
                Update(t);
            }
            
        }

        private void Update(string text)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate ()
            {
                ContentText.Text = text;
            });
        }

    }

    class HTTPManager()
    {
        public void Request()
        {
            NotificationCenter.DefaultCenter().Post("resuest.start");

            Task task = new Task(GetUserInfo);
            task.Start();

            Task task2 = new Task(GetUserInfo2);
            task2.Start();
        } 

        private void GetUserInfo()
        {

            Thread.Sleep(5000);

            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("name", "张三");
            dic.Add("age", "28");

            NotificationCenter.DefaultCenter().Post("resuest.success", dic);

        }

        private void GetUserInfo2()
        {

            Thread.Sleep(15000);

            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("name", "李四");
            dic.Add("age", "20");

            NotificationCenter.DefaultCenter().Post("resuest2.success", dic);
        }

    }

}