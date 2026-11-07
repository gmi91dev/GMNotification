using GMNotification;

namespace NotificationSample.Notifications
{
    /// <summary>
    /// 集中声明通知名，避免字符串散落在各处导致拼写错误。
    /// </summary>
    public static class NotificationNames
    {
        public static readonly NotificationName RequestStarted = new("request.started");
        public static readonly NotificationName RequestSucceeded = new("request.succeeded");
        public static readonly NotificationName RequestFailed = new("request.failed");

        /// <summary>异常隔离演示：一个观察者抛异常，另一个观察者仍应收到通知。</summary>
        public static readonly NotificationName DemoCallbackError = new("demo.callback.error");

        /// <summary>同名覆盖演示：同一观察者重复注册同名通知，只有最后一次生效。</summary>
        public static readonly NotificationName DemoOverride = new("demo.override");
    }
}
