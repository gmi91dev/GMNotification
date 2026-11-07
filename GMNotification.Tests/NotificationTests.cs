namespace GMNotification.Tests;

public sealed class NotificationTests : NotificationCenterTestBase
{
    [Fact]
    public void Constructor_WithNameOnly_LeavesUserInfoNull()
    {
        var notification = new Notification("user.login");

        Assert.Equal("user.login", notification.Name);
        Assert.Null(notification.UserInfo);
    }

    [Fact]
    public void Constructor_WithData_ExposesSameReference()
    {
        var data = new object();
        var notification = new Notification("user.login", data);

        Assert.Equal("user.login", notification.Name);
        Assert.Same(data, notification.UserInfo);
    }

    [Fact]
    public void ToString_ReturnsNameWhenNoData()
    {
        var notification = new Notification("plain");

        Assert.Equal("plain", notification.ToString());
    }

    [Fact]
    public void ToString_IncludesDataWhenPresent()
    {
        var notification = new Notification("with.data", new DisplayText("Alice"));

        Assert.Equal("with.data (Alice)", notification.ToString());
    }

    [Fact]
    public void NameAndUserInfo_AreReadOnly()
    {
        var name = typeof(Notification).GetProperty(nameof(Notification.Name));
        var userInfo = typeof(Notification).GetProperty(nameof(Notification.UserInfo));

        Assert.NotNull(name);
        Assert.NotNull(userInfo);
        Assert.False(name.CanWrite);
        Assert.False(userInfo.CanWrite);
    }

    [Fact]
    public void Post_NotificationInstanceIsSharedAcrossObservers()
    {
        var first = new object();
        var second = new object();
        Notification? firstNotification = null;
        Notification? secondNotification = null;

        Center.AddObserver(first, "shared", n => firstNotification = n);
        Center.AddObserver(second, "shared", n => secondNotification = n);

        Center.Post("shared", 42);

        Assert.NotNull(firstNotification);
        Assert.Same(firstNotification, secondNotification);
    }

    [Fact]
    public void Post_CreatesNewNotificationForEachPost()
    {
        var observer = new object();
        Notification? first = null;
        Notification? second = null;

        Center.AddObserver(observer, "each.post", n => first ??= n);
        Center.Post("each.post", 1);

        Center.RemoveObserver(observer);
        Center.AddObserver(observer, "each.post", n => second = n);
        Center.Post("each.post", 2);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
        Assert.Equal(1, Assert.IsType<int>(first.UserInfo));
        Assert.Equal(2, Assert.IsType<int>(second.UserInfo));
    }

    private sealed class DisplayText(string text)
    {
        public override string ToString() => text;
    }
}
