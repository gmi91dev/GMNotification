namespace GMNotification.Tests;

public sealed class NotificationObserverTests
{
    [Fact]
    public void IsEmpty_IsTrueInitially()
    {
        var observer = new NotificationObserver(new object());

        Assert.True(observer.IsEmpty);
    }

    [Fact]
    public void Observer_ReturnsSameReference()
    {
        var host = new object();
        var observer = new NotificationObserver(host);

        Assert.Same(host, observer.Observer);
    }

    [Fact]
    public void AddNotification_MakesObserverNonEmpty()
    {
        var observer = new NotificationObserver(new object());

        observer.AddNotification("name", _ => { });

        Assert.False(observer.IsEmpty);
    }

    [Fact]
    public void GetCallback_ReturnsRegisteredCallback()
    {
        var observer = new NotificationObserver(new object());
        Action<Notification> callback = _ => { };

        observer.AddNotification("name", callback);

        Assert.Same(callback, observer.GetCallback("name"));
    }

    [Fact]
    public void GetCallback_UnknownName_ReturnsNull()
    {
        var observer = new NotificationObserver(new object());

        Assert.Null(observer.GetCallback("missing"));
    }

    [Fact]
    public void AddNotification_SameName_ReplacesCallback()
    {
        var observer = new NotificationObserver(new object());
        Action<Notification> first = _ => { };
        Action<Notification> second = _ => { };

        observer.AddNotification("name", first);
        observer.AddNotification("name", second);

        Assert.Same(second, observer.GetCallback("name"));
    }

    [Fact]
    public void RemoveNotification_RemovesOnlyNamedCallback()
    {
        var observer = new NotificationObserver(new object());
        Action<Notification> first = _ => { };
        Action<Notification> second = _ => { };

        observer.AddNotification("one", first);
        observer.AddNotification("two", second);

        observer.RemoveNotification("one");

        Assert.Null(observer.GetCallback("one"));
        Assert.Same(second, observer.GetCallback("two"));
        Assert.False(observer.IsEmpty);
    }

    [Fact]
    public void RemoveNotification_UnknownName_DoesNotThrow()
    {
        var observer = new NotificationObserver(new object());

        observer.RemoveNotification("missing");

        Assert.True(observer.IsEmpty);
    }

    [Fact]
    public void RemoveNotification_WhenLastNotificationRemoved_IsEmptyAgain()
    {
        var observer = new NotificationObserver(new object());
        observer.AddNotification("name", _ => { });

        observer.RemoveNotification("name");

        Assert.True(observer.IsEmpty);
    }

    [Fact]
    public void ConcurrentAddRemoveGet_DoesNotThrow()
    {
        var observer = new NotificationObserver(new object());

        Parallel.For(0, 500, i =>
        {
            string name = $"name.{i % 10}";
            observer.AddNotification(name, _ => { });
            observer.GetCallback(name);

            if (i % 3 == 0)
            {
                observer.RemoveNotification(name);
            }
        });
    }
}
