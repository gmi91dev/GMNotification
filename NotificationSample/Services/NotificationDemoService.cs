using GMNotification;
using NotificationSample.Notifications;

namespace NotificationSample.Services
{
    /// <summary>
    /// 集中封装示例用的两个通知行为：异常隔离与同名覆盖。
    /// 演示逻辑不再散落在窗口代码中。
    /// </summary>
    public sealed class NotificationDemoService : IDisposable
    {
        private readonly NotificationCenter _center;
        private readonly Action<string> _log;
        private readonly object _throwingObserver = new();
        private readonly object _secondaryObserver = new();
        private bool _disposed;

        public NotificationDemoService(NotificationCenter center, Action<string> log)
        {
            _center = center ?? throw new ArgumentNullException(nameof(center));
            _log = log ?? throw new ArgumentNullException(nameof(log));

            // 异常隔离：第一个观察者抛异常，第二个观察者仍应收到通知
            _center.AddObserver(_throwingObserver, NotificationNames.DemoCallbackError, OnThrowingCallback);
            _center.AddObserver(_secondaryObserver, NotificationNames.DemoCallbackError, OnSecondaryCallback);

            // 同名覆盖：同一观察者注册两次同名通知，只有最后一次注册的回调生效
            _center.AddObserver(_secondaryObserver, NotificationNames.DemoOverride, OnOverriddenCallback);
            _center.AddObserver(_secondaryObserver, NotificationNames.DemoOverride, OnOverridingCallback);
        }

        public void TriggerCallbackError()
        {
            _log($"派发 {NotificationNames.DemoCallbackError}：第一个观察者会抛异常，第二个观察者仍应收到");
            _center.Post(NotificationNames.DemoCallbackError, "异常隔离演示");
        }

        public void TriggerOverride()
        {
            _log($"派发 {NotificationNames.DemoOverride}：该观察者注册了两次同名通知，只有后注册的回调会触发");
            _center.Post(NotificationNames.DemoOverride);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _center.RemoveObserver(_throwingObserver);
            _center.RemoveObserver(_secondaryObserver);
        }

        private static void OnThrowingCallback(Notification notification)
            => throw new InvalidOperationException($"模拟回调异常（{notification.Name}）");

        private void OnSecondaryCallback(Notification notification)
            => _log($"第二个观察者仍收到 {notification.Name}，说明前一个回调的异常已被隔离");

        private void OnOverriddenCallback(Notification notification)
            => _log($"旧回调被触发（不应出现）：{notification.Name}");

        private void OnOverridingCallback(Notification notification)
            => _log($"后注册的回调 B 被触发：{notification.Name}（同名注册时最后一次生效）");
    }
}
