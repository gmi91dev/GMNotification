using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GMNotification.Notification;

namespace GMNotification
{
    public class Notification
    {
        public string Name { get;}
        public object? UserInfo => _UserInfo;
        private object? _UserInfo { get; set; }

        internal Action<Notification> CallbackAction { get; }

        internal Notification(string name)
        {
            this.Name = name;
            this.CallbackAction = a => { };
        }

        internal Notification(string name, Action<Notification> callbackAction)
        {
            this.Name = name;
            this.CallbackAction = callbackAction;
        }

        internal void SetUserInfo(object? obj)
        {
            _UserInfo = obj;
        }

    }

}
