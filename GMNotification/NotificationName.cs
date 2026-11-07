namespace GMNotification
{
    /// <summary>
    /// 类型安全的通知名称，避免裸字符串散落导致的拼写错误。
    /// 可与 <see cref="string"/> 隐式互转，因此现有字符串 API 仍然兼容。
    /// </summary>
    public readonly struct NotificationName : IEquatable<NotificationName>
    {
        /// <summary>
        /// 创建通知名称。
        /// </summary>
        /// <param name="value">非空的通知名称。</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> 为空字符串。</exception>
        public NotificationName(string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            Value = value;
        }

        /// <summary>
        /// 通知名称字符串。
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// 从字符串创建通知名称。
        /// </summary>
        public static implicit operator NotificationName(string value) => new(value);

        /// <summary>
        /// 转换为字符串。
        /// </summary>
        public static implicit operator string(NotificationName name) => name.Value;

        /// <inheritdoc />
        public bool Equals(NotificationName other)
            => string.Equals(Value, other.Value, StringComparison.Ordinal);

        /// <inheritdoc />
        public override bool Equals(object? obj)
            => obj is NotificationName other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
            => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        /// <summary>
        /// 判断两个通知名称是否相等（按序号比较，区分大小写）。
        /// </summary>
        public static bool operator ==(NotificationName left, NotificationName right) => left.Equals(right);

        /// <summary>
        /// 判断两个通知名称是否不相等。
        /// </summary>
        public static bool operator !=(NotificationName left, NotificationName right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => Value ?? string.Empty;
    }
}
