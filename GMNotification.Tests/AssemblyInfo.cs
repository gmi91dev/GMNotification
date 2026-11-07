using Xunit;

// 所有测试共享 NotificationCenter.DefaultCenter() 单例，关闭并行以避免相互干扰
[assembly: CollectionBehavior(DisableTestParallelization = true)]
