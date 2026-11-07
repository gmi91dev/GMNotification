using System.Diagnostics;

namespace GMNotification
{
    /// <summary>
    /// 全局通知中心，负责观察者的注册、注销与通知派发。
    /// </summary>
    public sealed class NotificationCenter : IDisposable
    {
        private static readonly Lazy<NotificationCenter> s_default =
            new(() => new NotificationCenter(), LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly object _syncRoot = new();
        private readonly List<NotificationObserver> _registeredObservers = new();

        private NotificationCenter()
        {
        }

        /// <summary>
        /// 某个回调抛出异常时触发。异常会被隔离，不会影响其余观察者继续接收通知。
        /// 若未订阅该事件，异常信息会写入 <see cref="Trace"/>。
        /// </summary>
        public event EventHandler<NotificationErrorEventArgs>? CallbackError;

        /// <summary>
        /// 获取全局唯一的通知中心实例。
        /// </summary>
        public static NotificationCenter DefaultCenter() => s_default.Value;

        /// <summary>
        /// 注册观察者。同一观察者对同名通知重复注册时，最后一次注册覆盖前一次。
        /// </summary>
        /// <param name="observer">观察者对象，用于后续注销时识别归属。</param>
        /// <param name="name">通知名称。</param>
        /// <param name="callbackAction">收到通知时执行的回调。</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="observer"/> 或 <paramref name="callbackAction"/> 为 <see langword="null"/>。
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> 为 <see langword="null"/> 或空字符串。</exception>
        public void AddObserver(object observer, string name, Action<Notification> callbackAction)
        {
            ArgumentNullException.ThrowIfNull(observer);
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(callbackAction);

            lock (_syncRoot)
            {
                NotificationObserver? target = FindObserver(observer);

                if (target is null)
                {
                    target = new NotificationObserver(observer);
                    _registeredObservers.Add(target);
                }

                target.AddNotification(name, callbackAction);
            }
        }

        /// <summary>
        /// 注册观察者（<see cref="NotificationName"/> 重载）。
        /// </summary>
        public void AddObserver(object observer, NotificationName name, Action<Notification> callbackAction)
            => AddObserver(observer, name.Value, callbackAction);

        /// <summary>
        /// 发送通知。回调在调用 <see cref="Post(string, object?)"/> 的线程上执行。
        /// </summary>
        /// <param name="name">通知名称。</param>
        /// <param name="userInfo">可选的数据对象。</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> 为 <see langword="null"/> 或空字符串。</exception>
        public void Post(string name, object? userInfo = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            NotificationObserver[] snapshot;
            lock (_syncRoot)
            {
                if (_registeredObservers.Count == 0)
                {
                    return;
                }

                snapshot = _registeredObservers.ToArray();
            }

            // 通知实例不可变，所有回调共享同一个实例即可
            Notification notification = new(name, userInfo);

            // 在锁外派发，回调内部可以安全地再次注册或注销
            foreach (NotificationObserver observer in snapshot)
            {
                Action<Notification>? callback = observer.GetCallback(name);
                if (callback is null)
                {
                    continue;
                }

                try
                {
                    callback(notification);
                }
                catch (Exception exception)
                {
                    // 隔离单个观察者的异常，保证其余观察者仍能收到通知
                    OnCallbackError(notification, observer.Observer, exception);
                }
            }
        }

        /// <summary>
        /// 发送通知（<see cref="NotificationName"/> 重载）。
        /// </summary>
        public void Post(NotificationName name, object? userInfo = null)
            => Post(name.Value, userInfo);

        /// <summary>
        /// 移除观察者。<paramref name="notificationName"/> 为空时移除该观察者的全部通知，
        /// 否则只移除指定名称的通知。
        /// </summary>
        /// <param name="observer">注册时传入的观察者对象。</param>
        /// <param name="notificationName">要移除的通知名称；为空时移除该观察者的全部通知。</param>
        /// <exception cref="ArgumentNullException"><paramref name="observer"/> 为 <see langword="null"/>。</exception>
        public void RemoveObserver(object observer, string notificationName = "")
        {
            ArgumentNullException.ThrowIfNull(observer);

            lock (_syncRoot)
            {
                NotificationObserver? target = FindObserver(observer);
                if (target is null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(notificationName))
                {
                    _registeredObservers.Remove(target);
                    return;
                }

                target.RemoveNotification(notificationName);

                // 观察者已没有任何通知时一并清理，避免留下空壳
                if (target.IsEmpty)
                {
                    _registeredObservers.Remove(target);
                }
            }
        }

        /// <summary>
        /// 按名称移除观察者的通知（<see cref="NotificationName"/> 重载）。
        /// </summary>
        public void RemoveObserver(object observer, NotificationName notificationName)
        {
            ArgumentException.ThrowIfNullOrEmpty(notificationName.Value);
            RemoveObserver(observer, notificationName.Value);
        }

        /// <summary>
        /// 移除全部观察者。
        /// </summary>
        public void RemoveAllObservers()
        {
            lock (_syncRoot)
            {
                _registeredObservers.Clear();
            }
        }

        /// <summary>
        /// 释放通知中心：清空所有观察者。之后仍可继续使用该实例。
        /// </summary>
        public void Dispose() => RemoveAllObservers();

        private NotificationObserver? FindObserver(object observer)
        {
            foreach (NotificationObserver candidate in _registeredObservers)
            {
                if (ReferenceEquals(candidate.Observer, observer))
                {
                    return candidate;
                }
            }

            return null;
        }

        private void OnCallbackError(Notification notification, object observer, Exception exception)
        {
            EventHandler<NotificationErrorEventArgs>? handler = CallbackError;

            if (handler is null)
            {
                Trace.TraceError(
                    $"[GMNotification] 回调执行失败，通知：{notification.Name}，观察者：{observer.GetType().FullName}，异常：{exception}");
                return;
            }

            try
            {
                handler(this, new NotificationErrorEventArgs(notification, observer, exception));
            }
            catch (Exception handlerException)
            {
                // 事件处理器自身出错时也不能中断派发流程
                Trace.TraceError($"[GMNotification] CallbackError 事件处理器执行失败：{handlerException}");
            }
        }
    }
}
