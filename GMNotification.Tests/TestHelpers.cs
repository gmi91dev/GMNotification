namespace GMNotification.Tests;

/// <summary>
/// 共享单例测试基类：每个测试前后清空观察者，避免测试之间相互影响。
/// </summary>
public abstract class NotificationCenterTestBase : IDisposable
{
    protected NotificationCenter Center { get; } = NotificationCenter.DefaultCenter();

    protected NotificationCenterTestBase()
    {
        Center.RemoveAllObservers();
    }

    public void Dispose()
    {
        Center.RemoveAllObservers();
    }
}

/// <summary>
/// 订阅 <see cref="NotificationCenter.CallbackError"/>，Dispose 时自动取消订阅。
/// </summary>
internal sealed class CallbackErrorSubscription : IDisposable
{
    private readonly NotificationCenter _center;
    private readonly EventHandler<NotificationErrorEventArgs> _handler;

    public CallbackErrorSubscription(NotificationCenter center, Action<NotificationErrorEventArgs> onError)
    {
        _center = center;
        _handler = (_, e) => onError(e);
        _center.CallbackError += _handler;
    }

    public void Dispose()
    {
        _center.CallbackError -= _handler;
    }
}
