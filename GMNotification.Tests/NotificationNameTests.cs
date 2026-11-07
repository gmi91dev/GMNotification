namespace GMNotification.Tests;

public sealed class NotificationNameTests : NotificationCenterTestBase
{
    [Fact]
    public void Constructor_WithValidValue_SetsValue()
    {
        var name = new NotificationName("user.login");

        Assert.Equal("user.login", name.Value);
    }

    [Fact]
    public void Constructor_WithNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new NotificationName(null!));
    }

    [Fact]
    public void Constructor_WithEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new NotificationName(string.Empty));
    }

    [Fact]
    public void Constructor_WithWhitespace_IsAllowed()
    {
        var name = new NotificationName(" ");

        Assert.Equal(" ", name.Value);
    }

    [Fact]
    public void ImplicitConversion_FromString()
    {
        NotificationName name = "user.login";

        Assert.Equal("user.login", name.Value);
    }

    [Fact]
    public void ImplicitConversion_ToString()
    {
        string value = new NotificationName("user.login");

        Assert.Equal("user.login", value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var name = new NotificationName("user.login");

        Assert.Equal("user.login", name.ToString());
    }

    [Fact]
    public void Default_ToString_ReturnsEmpty()
    {
        NotificationName name = default;

        Assert.Equal(string.Empty, name.ToString());
    }

    [Fact]
    public void Default_Value_IsNull()
    {
        Assert.Null(default(NotificationName).Value);
    }

    [Fact]
    public void Default_GetHashCode_IsZero()
    {
        Assert.Equal(0, default(NotificationName).GetHashCode());
    }

    [Fact]
    public void Equality_SameValue_IsTrue()
    {
        NotificationName first = "user.login";
        var second = new NotificationName("user.login");

        Assert.True(first == second);
        Assert.False(first != second);
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValue_IsFalse()
    {
        NotificationName first = "user.login";
        NotificationName second = "user.logout";

        Assert.False(first == second);
        Assert.True(first != second);
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Equality_IsCaseSensitive()
    {
        NotificationName lower = "user.login";
        NotificationName upper = "User.Login";

        Assert.False(lower == upper);
        Assert.False(lower.Equals(upper));
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        NotificationName name = "user.login";

        Assert.False(name.Equals((object?)null));
    }

    [Fact]
    public void Equals_WithNonNotificationName_ReturnsFalse()
    {
        NotificationName name = "user.login";

        Assert.False(name.Equals((object)"user.login"));
    }

    [Fact]
    public void DefaultEquality_Works()
    {
        Assert.True(default(NotificationName) == default);
        Assert.False(default(NotificationName) != default);
        Assert.False(default(NotificationName) == new NotificationName("name"));
        Assert.True(default(NotificationName) != new NotificationName("name"));
    }

    [Fact]
    public void CanBeUsedAsDictionaryKey_WithOrdinalSemantics()
    {
        var map = new Dictionary<NotificationName, int>
        {
            [new NotificationName("A")] = 1,
            [new NotificationName("a")] = 2,
        };

        Assert.Equal(2, map.Count);
        Assert.Equal(1, map["A"]);
        Assert.Equal(2, map["a"]);
    }

    [Fact]
    public void TypedOverloads_WorkWithObserverApis()
    {
        var observer = new object();
        NotificationName name = "typed.notification";
        int count = 0;

        Center.AddObserver(observer, name, _ => count++);
        Center.Post(name, 42);
        Assert.Equal(1, count);

        Center.RemoveObserver(observer, name);
        Center.Post(name);
        Assert.Equal(1, count);
    }

    [Fact]
    public void StringOverloads_StillWorkAlongsideTypedOnes()
    {
        var observer = new object();
        int count = 0;

        Center.AddObserver(observer, "string.notification", _ => count++);
        Center.Post("string.notification");
        Center.RemoveObserver(observer, "string.notification");
        Center.Post("string.notification");

        Assert.Equal(1, count);
    }
}
