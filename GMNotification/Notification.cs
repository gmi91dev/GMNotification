namespace GMNotification
{
    /// <summary>
    /// 通知的数据封装，对应一次「发出的消息」。
    /// 实例不可变，派发时可安全地在多个回调之间共享。
    /// </summary>
    public sealed class Notification
    {
        internal Notification(string name, object? userInfo = null)
        {
            Name = name;
            UserInfo = userInfo;
        }

        /// <summary>
        /// 通知名称，作为派发的标识。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 通知携带的可选负载。
        /// </summary>
        public object? UserInfo { get; }

        /// <inheritdoc />
        public override string ToString()
            => UserInfo is null ? Name : $"{Name} ({UserInfo})";
    }
}
