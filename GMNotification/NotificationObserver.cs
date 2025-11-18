using Accessibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMNotification
{
    internal class NotificationObserver
    {
        internal readonly object observer;
        internal List<Notification> RegisteredNotifications => _RegisteredNotifications;
        private List<Notification> _RegisteredNotifications { get; set; }

        internal NotificationObserver(object observer)
        {
            this.observer = observer;
            this._RegisteredNotifications = new List<Notification>();
        }

        internal void AddNotification(Notification notification)
        {
            Notification? noti = _RegisteredNotifications.Where(e => e.Name == notification.Name).ToList().FirstOrDefault();

            // 注册通知时，最后一次会覆盖之前的
            if (noti != null)
            {
                RemoveNotification(noti);
            }
            _RegisteredNotifications.Add(notification);
        }

        internal void RemoveNotification(string name)
        {
            Notification? noti = _RegisteredNotifications.Where(x => x.Name == name).FirstOrDefault();

            if (noti != null)
            {
                RemoveNotification(noti);
            }
        }

        internal void RemoveNotification(Notification notification)
        {
            _RegisteredNotifications.Remove(notification);
        }

        internal bool ContainsNotification(string name)
        {
            Notification? noti = _RegisteredNotifications.Where(s => s.Name == name).ToList().FirstOrDefault();
            if (noti != null)
            {
                return true;
            }

            return false;
        }

        internal void Post(string name, object? userInfo = null)
        {
            Notification? noti = _RegisteredNotifications.Where(s => s.Name == name).ToList().FirstOrDefault();
            if (noti != null)
            {
                Notification newNoti = new Notification(name);
                newNoti.SetUserInfo(userInfo);
                // 传一个新的实例，防止操作污染
                noti.CallbackAction(newNoti);
            }
        }

    }
}
