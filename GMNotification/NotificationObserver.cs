namespace GMNotification
{
    /// <summary>
    /// 封装单个宿主对象所注册的全部通知。
    /// </summary>
    internal sealed class NotificationObserver
    {
        private readonly object _syncRoot = new();
        private readonly Dictionary<string, Action<Notification>> _callbacks = new(StringComparer.Ordinal);

        internal NotificationObserver(object observer)
        {
            Observer = observer;
        }

        internal object Observer { get; }

        internal bool IsEmpty
        {
            get
            {
                lock (_syncRoot)
                {
                    return _callbacks.Count == 0;
                }
            }
        }

        internal void AddNotification(string name, Action<Notification> callback)
        {
            lock (_syncRoot)
            {
                // 同一观察者对同名通知重复注册时，最后一次覆盖之前的
                _callbacks[name] = callback;
            }
        }

        internal void RemoveNotification(string name)
        {
            lock (_syncRoot)
            {
                _callbacks.Remove(name);
            }
        }

        internal Action<Notification>? GetCallback(string name)
        {
            lock (_syncRoot)
            {
                return _callbacks.TryGetValue(name, out Action<Notification>? callback)
                    ? callback
                    : null;
            }
        }
    }
}
