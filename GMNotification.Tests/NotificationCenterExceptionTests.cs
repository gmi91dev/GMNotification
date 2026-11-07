namespace GMNotification.Tests;

public sealed class NotificationCenterExceptionTests : NotificationCenterTestBase
{
    [Fact]
    public void CallbackThrows_DoesNotPreventOtherObservers()
    {
        bool received = false;

        Center.AddObserver(new object(), "boom", _ => throw new InvalidOperationException("boom"));
        Center.AddObserver(new object(), "boom", _ => received = true);

        using var subscription = new CallbackErrorSubscription(Center, _ => { });
        Center.Post("boom");

        Assert.True(received);
    }

    [Fact]
    public void CallbackThrows_RaisesCallbackErrorWithCorrectArgs()
    {
        var throwing = new object();
        var receiving = new object();
        Notification? received = null;
        NotificationErrorEventArgs? error = null;
        using var subscription = new CallbackErrorSubscription(Center, e => error = e);

        Center.AddObserver(throwing, "boom", _ => throw new InvalidOperationException("boom"));
        Center.AddObserver(receiving, "boom", n => received = n);

        Center.Post("boom", 7);

        Assert.NotNull(error);
        Assert.Same(throwing, error.Observer);
        Assert.Equal("boom", error.Notification.Name);
        Assert.Equal(7, error.Notification.UserInfo);
        Assert.IsType<InvalidOperationException>(error.Exception);
        Assert.Same(received, error.Notification);
    }

    [Fact]
    public void MultipleCallbacksThrow_ErrorRaisedForEach_OtherObserversStillInvoked()
    {
        var errors = new List<NotificationErrorEventArgs>();
        int received = 0;
        using var subscription = new CallbackErrorSubscription(Center, errors.Add);

        Center.AddObserver(new object(), "boom", _ => throw new InvalidOperationException("first"));
        Center.AddObserver(new object(), "boom", _ => throw new ArgumentException("second"));
        Center.AddObserver(new object(), "boom", _ => received++);

        Center.Post("boom");

        Assert.Equal(2, errors.Count);
        Assert.IsType<InvalidOperationException>(errors[0].Exception);
        Assert.IsType<ArgumentException>(errors[1].Exception);
        Assert.Equal(1, received);
    }

    [Fact]
    public void CallbackError_NoSubscriber_DoesNotThrow()
    {
        Center.AddObserver(new object(), "boom", _ => throw new InvalidOperationException("boom"));

        Center.Post("boom");
    }

    [Fact]
    public void CallbackError_MultipleSubscribers_AllInvoked()
    {
        int first = 0;
        int second = 0;
        using var firstSubscription = new CallbackErrorSubscription(Center, _ => first++);
        using var secondSubscription = new CallbackErrorSubscription(Center, _ => second++);

        Center.AddObserver(new object(), "boom", _ => throw new InvalidOperationException("boom"));
        Center.Post("boom");

        Assert.Equal(1, first);
        Assert.Equal(1, second);
    }

    [Fact]
    public void CallbackError_UnsubscribedHandler_NotInvoked()
    {
        int calls = 0;
        var subscription = new CallbackErrorSubscription(Center, _ => calls++);
        subscription.Dispose();

        Center.AddObserver(new object(), "boom", _ => throw new InvalidOperationException("boom"));
        Center.Post("boom");

        Assert.Equal(0, calls);
    }

    [Fact]
    public void CallbackErrorHandlerThrows_DoesNotBreakDispatch()
    {
        bool received = false;
        using var subscription = new CallbackErrorSubscription(Center, _ => throw new Exception("handler failed"));

        Center.AddObserver(new object(), "boom", _ => throw new InvalidOperationException("callback failed"));
        Center.AddObserver(new object(), "boom", _ => received = true);

        Center.Post("boom");

        Assert.True(received);
    }

    [Fact]
    public void CallbackErrorHandler_CanPostAnotherNotification()
    {
        int recovered = 0;
        Center.AddObserver(new object(), "recovered", _ => recovered++);
        using var subscription = new CallbackErrorSubscription(Center, _ => Center.Post("recovered"));

        Center.AddObserver(new object(), "boom", _ => throw new InvalidOperationException("boom"));
        Center.Post("boom");

        Assert.Equal(1, recovered);
    }

    [Fact]
    public void CallbackThrows_DoesNotBreakSubsequentPosts()
    {
        var observer = new object();
        int count = 0;
        using var subscription = new CallbackErrorSubscription(Center, _ => { });

        Center.AddObserver(observer, "boom", _ => throw new InvalidOperationException("boom"));
        Center.Post("boom");

        Center.RemoveObserver(observer);
        Center.AddObserver(observer, "boom", _ => count++);
        Center.Post("boom");

        Assert.Equal(1, count);
    }
}
