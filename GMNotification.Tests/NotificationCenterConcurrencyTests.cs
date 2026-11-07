namespace GMNotification.Tests;

public sealed class NotificationCenterConcurrencyTests : NotificationCenterTestBase
{
    [Fact]
    public void DefaultCenter_ConcurrentAccess_ReturnsSameInstance()
    {
        var expected = NotificationCenter.DefaultCenter();
        var instances = new NotificationCenter[500];

        Parallel.For(0, instances.Length, i => instances[i] = NotificationCenter.DefaultCenter());

        Assert.All(instances, instance => Assert.Same(expected, instance));
    }

    [Fact]
    public void ConcurrentPosts_InvokeObserverExactlyOncePerPost()
    {
        const int threadCount = 8;
        const int postsPerThread = 500;
        int received = 0;

        Center.AddObserver(new object(), "hot", _ => Interlocked.Increment(ref received));

        Parallel.For(0, threadCount, _ =>
        {
            for (int i = 0; i < postsPerThread; i++)
            {
                Center.Post("hot");
            }
        });

        Assert.Equal(threadCount * postsPerThread, received);
    }

    [Fact]
    public void ConcurrentAddRemovePost_DifferentNames_AllPostsDelivered()
    {
        const int iterations = 500;
        int received = 0;

        Parallel.For(0, iterations, i =>
        {
            string name = $"concurrent.{i}";
            var observer = new object();

            Center.AddObserver(observer, name, _ => Interlocked.Increment(ref received));
            Center.Post(name, i);
            Center.RemoveObserver(observer);
        });

        Assert.Equal(iterations, received);
    }

    [Fact]
    public void ConcurrentAddSameObserverSameName_LeavesSingleCallback()
    {
        var observer = new object();
        int received = 0;

        Parallel.For(0, 1000, _ =>
            Center.AddObserver(observer, "same", _ => Interlocked.Increment(ref received)));

        Center.Post("same");

        Assert.Equal(1, received);
    }

    [Fact]
    public void ConcurrentRemoveAllAndPost_DoesNotThrow()
    {
        Center.AddObserver(new object(), "x", _ => { });

        Parallel.Invoke(
            () =>
            {
                for (int i = 0; i < 500; i++)
                {
                    Center.Post("x");
                }
            },
            () =>
            {
                for (int i = 0; i < 500; i++)
                {
                    Center.RemoveAllObservers();
                }
            });
    }

    [Fact]
    public void ConcurrentCallbackErrorSubscriptions_DoNotThrow()
    {
        Center.AddObserver(new object(), "boom", _ => throw new InvalidOperationException("boom"));

        Parallel.For(0, 200, _ =>
        {
            EventHandler<NotificationErrorEventArgs> handler = (_, _) => { };
            Center.CallbackError += handler;
            Center.Post("boom");
            Center.CallbackError -= handler;
        });
    }
}
