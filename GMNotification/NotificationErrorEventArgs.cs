namespace GMNotification
{
    /// <summary>
    /// 观察者回调执行过程中抛出异常时的事件参数。
    /// </summary>
    public sealed class NotificationErrorEventArgs : EventArgs
    {
        internal NotificationErrorEventArgs(Notification notification, object observer, Exception exception)
        {
            Notification = notification;
            Observer = observer;
            Exception = exception;
        }

        /// <summary>
        /// 正在派发的通知。
        /// </summary>
        public Notification Notification { get; }

        /// <summary>
        /// 回调所属的观察者对象。
        /// </summary>
        public object Observer { get; }

        /// <summary>
        /// 回调抛出的异常。
        /// </summary>
        public Exception Exception { get; }
    }
}
