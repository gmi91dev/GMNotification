namespace GMNotification.Tests;

public sealed class NotificationCenterTests : NotificationCenterTestBase
{
    [Fact]
    public void DefaultCenter_ReturnsSameInstance()
    {
        Assert.Same(NotificationCenter.DefaultCenter(), NotificationCenter.DefaultCenter());
    }

    [Fact]
    public void AddObserver_NullObserver_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Center.AddObserver(null!, "name", _ => { }));
    }

    [Fact]
    public void AddObserver_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Center.AddObserver(new object(), (string)null!, _ => { }));
    }

    [Fact]
    public void AddObserver_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Center.AddObserver(new object(), string.Empty, _ => { }));
    }

    [Fact]
    public void AddObserver_NullCallback_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Center.AddObserver(new object(), "name", null!));
    }

    [Fact]
    public void AddObserver_WhitespaceName_IsAllowed()
    {
        var observer = new object();
        int count = 0;

        Center.AddObserver(observer, " ", _ => count++);
        Center.Post(" ");

        Assert.Equal(1, count);
    }

    [Fact]
    public void AddObserver_TypedNameOverload_RegistersSameAsString()
    {
        var observer = new object();
        int count = 0;
        NotificationName name = "typed.name";

        Center.AddObserver(observer, name, _ => count++);
        Center.Post("typed.name");
        Center.Post(name);

        Assert.Equal(2, count);
    }

    [Fact]
    public void AddObserver_DefaultTypedName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Center.AddObserver(new object(), default(NotificationName), _ => { }));
    }

    [Fact]
    public void Post_NoObservers_DoesNotThrow()
    {
        Center.Post("empty");
    }

    [Fact]
    public void Post_NoMatchingObserver_DoesNotThrow()
    {
        Center.Post("nobody.listens", 42);
    }

    [Fact]
    public void Post_InvalidName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Center.Post((string)null!));
        Assert.Throws<ArgumentException>(() => Center.Post(string.Empty));
        Assert.Throws<ArgumentNullException>(() => Center.Post(default(NotificationName)));
    }

    [Fact]
    public void Post_InvokesAllMatchingObservers_WithData()
    {
        var first = new object();
        var second = new object();
        object? firstData = null;
        object? secondData = null;

        Center.AddObserver(first, "user.login", n => firstData = n.UserInfo);
        Center.AddObserver(second, "user.login", n => secondData = n.UserInfo);

        var data = new object();
        Center.Post("user.login", data);

        Assert.Same(data, firstData);
        Assert.Same(data, secondData);
    }

    [Fact]
    public void Post_NullData_DeliversNull()
    {
        var observer = new object();
        object? data = new object();

        Center.AddObserver(observer, "null.data", n => data = n.UserInfo);
        Center.Post("null.data");

        Assert.Null(data);
    }

    [Fact]
    public void Post_ValueTypeData_DeliversValue()
    {
        var observer = new object();
        object? data = null;

        Center.AddObserver(observer, "value.data", n => data = n.UserInfo);
        Center.Post("value.data", 42);

        Assert.Equal(42, Assert.IsType<int>(data));
    }

    [Fact]
    public void Post_ReferenceData_DeliversSameReference()
    {
        var observer = new object();
        object? data = null;
        var expected = new object();

        Center.AddObserver(observer, "reference.data", n => data = n.UserInfo);
        Center.Post("reference.data", expected);

        Assert.Same(expected, data);
    }

    [Fact]
    public void Post_IsCaseSensitive()
    {
        var observer = new object();
        int count = 0;

        Center.AddObserver(observer, "CaseSensitive", _ => count++);
        Center.Post("casesensitive");
        Assert.Equal(0, count);

        Center.Post("CaseSensitive");
        Assert.Equal(1, count);
    }

    [Fact]
    public void Post_PreservesRegistrationOrder()
    {
        var order = new List<int>();

        Center.AddObserver(new object(), "order", _ => order.Add(1));
        Center.AddObserver(new object(), "order", _ => order.Add(2));
        Center.AddObserver(new object(), "order", _ => order.Add(3));

        Center.Post("order");

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    [Fact]
    public void Post_DoesNotInvokeObserversRegisteredForOtherNames()
    {
        int firstCount = 0;
        int secondCount = 0;

        Center.AddObserver(new object(), "first", _ => firstCount++);
        Center.AddObserver(new object(), "second", _ => secondCount++);

        Center.Post("first");

        Assert.Equal(1, firstCount);
        Assert.Equal(0, secondCount);
    }

    [Fact]
    public void Post_ReentrantPost_Works()
    {
        var observer = new object();
        int outerCount = 0;
        int innerCount = 0;

        Center.AddObserver(observer, "outer", _ =>
        {
            outerCount++;
            Center.Post("inner");
        });
        Center.AddObserver(observer, "inner", _ => innerCount++);

        Center.Post("outer");

        Assert.Equal(1, outerCount);
        Assert.Equal(1, innerCount);
    }

    [Fact]
    public void Post_CallbackCanAddObserver_NewObserverReceivesNextPostOnly()
    {
        var first = new object();
        var second = new object();
        int secondCount = 0;

        Center.AddObserver(first, "adding", _ =>
            Center.AddObserver(second, "adding", __ => secondCount++));

        Center.Post("adding");
        Assert.Equal(0, secondCount);

        Center.Post("adding");
        Assert.Equal(1, secondCount);
    }

    [Fact]
    public void Post_CallbackCanRemoveLaterObserver_LaterObserverStillReceivesCurrentDispatch()
    {
        var first = new object();
        var second = new object();
        int secondCount = 0;

        Center.AddObserver(first, "removing", _ => Center.RemoveObserver(second));
        Center.AddObserver(second, "removing", _ => secondCount++);

        // 派发使用观察者快照：本次派发仍会执行 second 的回调
        Center.Post("removing");
        Assert.Equal(1, secondCount);

        // 后续派发不再包含已注销的观察者
        Center.Post("removing");
        Assert.Equal(1, secondCount);
    }

    [Fact]
    public void Post_CallbackCanRemoveItself_NoFurtherInvocation()
    {
        var observer = new object();
        int count = 0;

        Center.AddObserver(observer, "self.remove", _ =>
        {
            count++;
            Center.RemoveObserver(observer);
        });

        Center.Post("self.remove");
        Center.Post("self.remove");

        Assert.Equal(1, count);
    }

    [Fact]
    public void SameObserver_SameName_LastRegistrationWins()
    {
        var observer = new object();
        int oldCount = 0;
        int newCount = 0;

        Center.AddObserver(observer, "duplicate", _ => oldCount++);
        Center.AddObserver(observer, "duplicate", _ => newCount++);

        Center.Post("duplicate");

        Assert.Equal(0, oldCount);
        Assert.Equal(1, newCount);
    }

    [Fact]
    public void SameObserver_DifferentNames_AllCallbacksInvoked()
    {
        var observer = new object();
        int started = 0;
        int finished = 0;

        Center.AddObserver(observer, "job.started", _ => started++);
        Center.AddObserver(observer, "job.finished", _ => finished++);

        Center.Post("job.started");
        Center.Post("job.finished");

        Assert.Equal(1, started);
        Assert.Equal(1, finished);
    }

    [Fact]
    public void RemoveObserver_ByName_RemovesOnlyThatNotification()
    {
        var observer = new object();
        int removed = 0;
        int kept = 0;

        Center.AddObserver(observer, "remove.me", _ => removed++);
        Center.AddObserver(observer, "keep.me", _ => kept++);

        Center.RemoveObserver(observer, "remove.me");
        Center.Post("remove.me");
        Center.Post("keep.me");

        Assert.Equal(0, removed);
        Assert.Equal(1, kept);
    }

    [Fact]
    public void RemoveObserver_WithoutName_RemovesAllNotificationsOfObserver()
    {
        var observer = new object();
        int count = 0;

        Center.AddObserver(observer, "one", _ => count++);
        Center.AddObserver(observer, "two", _ => count++);

        Center.RemoveObserver(observer);

        Center.Post("one");
        Center.Post("two");

        Assert.Equal(0, count);
    }

    [Fact]
    public void RemoveObserver_UnknownObserver_DoesNotThrow()
    {
        Center.RemoveObserver(new object());
        Center.RemoveObserver(new object(), "unknown");
    }

    [Fact]
    public void RemoveObserver_UnknownNotificationName_DoesNotThrow()
    {
        var observer = new object();
        int count = 0;

        Center.AddObserver(observer, "kept", _ => count++);
        Center.RemoveObserver(observer, "missing");
        Center.Post("kept");

        Assert.Equal(1, count);
    }

    [Fact]
    public void RemoveObserver_NullObserver_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Center.RemoveObserver(null!));
    }

    [Fact]
    public void RemoveObserver_TypedName_RemovesNotification()
    {
        var observer = new object();
        int count = 0;
        NotificationName name = "typed.remove";

        Center.AddObserver(observer, name, _ => count++);
        Center.RemoveObserver(observer, name);
        Center.Post(name);

        Assert.Equal(0, count);
    }

    [Fact]
    public void RemoveObserver_DefaultTypedName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Center.RemoveObserver(new object(), default(NotificationName)));
    }

    [Fact]
    public void RemoveObserver_DoesNotAffectOtherObservers()
    {
        var first = new object();
        var second = new object();
        int secondCount = 0;

        Center.AddObserver(first, "shared", _ => { });
        Center.AddObserver(second, "shared", _ => secondCount++);

        Center.RemoveObserver(first);
        Center.Post("shared");

        Assert.Equal(1, secondCount);
    }

    [Fact]
    public void RemoveObserver_ThenReAdd_Works()
    {
        var observer = new object();
        int count = 0;

        Center.AddObserver(observer, "readd", _ => count++);
        Center.RemoveObserver(observer, "readd");
        Center.Post("readd");
        Assert.Equal(0, count);

        Center.AddObserver(observer, "readd", _ => count++);
        Center.Post("readd");
        Assert.Equal(1, count);
    }

    [Fact]
    public void RemoveObserver_UsesReferenceEquality_NotEqualsOverride()
    {
        var first = new AlwaysEqual();
        var second = new AlwaysEqual();
        int firstCount = 0;
        int secondCount = 0;

        Center.AddObserver(first, "reference", _ => firstCount++);
        Center.AddObserver(second, "reference", _ => secondCount++);

        Center.RemoveObserver(first, "reference");
        Center.Post("reference");

        Assert.Equal(0, firstCount);
        Assert.Equal(1, secondCount);
    }

    [Fact]
    public void RemoveAllObservers_ClearsEverything()
    {
        var observer = new object();
        int count = 0;

        Center.AddObserver(observer, "clear", _ => count++);
        Center.RemoveAllObservers();
        Center.Post("clear");

        Assert.Equal(0, count);
    }

    [Fact]
    public void RemoveAllObservers_WhenEmpty_DoesNotThrow()
    {
        Center.RemoveAllObservers();
        Center.RemoveAllObservers();
    }

    [Fact]
    public void Dispose_ClearsObservers_AndCenterRemainsUsable()
    {
        var observer = new object();
        int count = 0;

        Center.AddObserver(observer, "dispose", _ => count++);
        Center.Dispose();
        Center.Post("dispose");

        Assert.Equal(0, count);

        Center.AddObserver(observer, "dispose", _ => count++);
        Center.Post("dispose");

        Assert.Equal(1, count);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        Center.Dispose();
        Center.Dispose();
    }

    [Fact]
    public void AddObserver_AfterRemoveAll_Works()
    {
        var observer = new object();
        int count = 0;

        Center.RemoveAllObservers();
        Center.AddObserver(observer, "again", _ => count++);
        Center.Post("again");

        Assert.Equal(1, count);
    }

    private sealed class AlwaysEqual
    {
        public override bool Equals(object? obj) => obj is AlwaysEqual;

        public override int GetHashCode() => 0;
    }
}
