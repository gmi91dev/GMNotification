
using Microsoft.Windows.Themes;

namespace GMNotification
{
    public class NotificationCenter: IDisposable
    {
        private List<NotificationObserver> _RegisteredObservers { get; set; }

        #region 单例

        private static NotificationCenter? _instance;
        private static readonly object locker = new object();
        
        private NotificationCenter()
        {
            _RegisteredObservers = new List<NotificationObserver>();
        }

        // 单例
        public static NotificationCenter DefaultCenter()
        {
            if (_instance == null)
            {
                lock (locker)
                {
                    if (_instance == null)
                    {
                        _instance = new NotificationCenter();
                    }
                }
            }
            return _instance;
        }

        #endregion

        public void AddObserver(object observer, string name, Action<Notification> callbackAction)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            if (_RegisteredObservers.Count > 0)
            {
                NotificationObserver? ob = _RegisteredObservers.Where(e => e.observer.Equals(observer)).ToList().FirstOrDefault();

                if (ob != null && _RegisteredObservers.Contains(ob))
                {
                    Notification noti = new Notification(name, callbackAction);
                    ob.AddNotification(noti);
                }
                else
                {
                    NotificationObserver obser = new NotificationObserver(observer);
                    Notification noti = new Notification(name, callbackAction);
                    obser.AddNotification(noti);
                    _RegisteredObservers.Add(obser);
                }
            }else
            {
                NotificationObserver obser = new NotificationObserver(observer);
                Notification noti = new Notification(name, callbackAction);
                obser.AddNotification(noti);
                _RegisteredObservers.Add(obser);
            }
        }

        public void Post(string name, object? userInfo = null)
        {
            var obs = _RegisteredObservers.Where(e => e.ContainsNotification(name)).ToList();

            if (obs.Count <= 0) { return; }

            foreach (var observer in obs)
            {
                observer.Post(name, userInfo);
            }
        }

        public void RemoveObserver(object observer, string notificationName = "")
        {
            if (observer == null)
            {
                throw new ArgumentNullException("observer is null");
            }

            if (_RegisteredObservers.Count <= 0) { return; }

            NotificationObserver? ob = _RegisteredObservers.Where(e => e.observer.Equals(observer)).ToList().FirstOrDefault();

            if (ob == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(notificationName))
            {
                ob.RemoveNotification(notificationName);
            }else
            {
                _RegisteredObservers.Remove(ob);
            }   
        }

        // 移除所有观察者
        public void RemoveAllObservers()
        {
            _RegisteredObservers.Clear();
        }

        public void Dispose()
        {
            RemoveAllObservers();
        }
    }

}
