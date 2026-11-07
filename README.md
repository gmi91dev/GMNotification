# GMNotification

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/GMNotification.svg)](https://www.nuget.org/packages/GMNotification)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/gmi91dev/GMNotification/blob/main/LICENSE)

一个面向 .NET 8 的轻量级通知中心（Notification Center），参考 iOS/macOS `NSNotificationCenter` 的设计。核心库不依赖 WPF，可在控制台、WPF、WinForms、ASP.NET Core 等任意 .NET 8 应用中复用。

## 特性

- **单例通知中心**：`NotificationCenter.DefaultCenter()` 提供全局统一入口
- **按名称派发**：支持字符串通知名和类型安全的 `NotificationName`
- **对象 + 通知绑定**：按「观察者对象 + 通知名称」注册与注销
- **任意数据**：`Notification.UserInfo` 可携带任意 `object`
- **同名覆盖**：同一观察者对同名通知重复注册时，最后一次注册生效
- **线程安全**：注册、注销、派发均可从任意线程调用
- **异常隔离**：单个回调抛异常不会中断其他观察者
- **不可变通知**：`Notification` 一次派发只创建一次，可被所有回调安全共享
- **零 WPF 依赖**：核心库目标框架为 `net8.0`

## 环境要求

- .NET SDK 8.0 或更高
- 核心库可在任何支持 .NET 8 的平台运行
- 示例项目 `NotificationSample` 需要 Windows（WPF）

## 安装

已发布到 nuget.org：

```bash
dotnet add package GMNotification
```

指定版本：

```bash
dotnet add package GMNotification --version 0.0.1
```

## 快速开始

### 1. 声明通知名

推荐把通知名集中声明为 `NotificationName`：

```csharp
using GMNotification;

public static class NotificationNames
{
    public static readonly NotificationName UserLogin = new("user.login");
}
```

`NotificationName` 是 `readonly struct`，可与 `string` 隐式互转，因此也可以直接使用字符串：

```csharp
var userInfo = new User("Alice");
NotificationCenter.DefaultCenter().Post("user.login", userInfo);
```

### 2. 注册、派发与注销

```csharp
using GMNotification;

public sealed class LoginViewModel : IDisposable
{
    private readonly NotificationCenter _center = NotificationCenter.DefaultCenter();

    public LoginViewModel()
    {
        _center.AddObserver(this, NotificationNames.UserLogin, OnUserLogin);
    }

    public void SimulateLogin()
    {
        var user = new User("Alice");
        _center.Post(NotificationNames.UserLogin, user);
    }

    private void OnUserLogin(Notification notification)
    {
        if (notification.UserInfo is User user)
        {
            Console.WriteLine($"{notification.Name}: {user.Name}");
        }
    }

    public void Dispose()
    {
        _center.RemoveObserver(this);
    }
}

public sealed record User(string Name);
```

### 3. 取消订阅

```csharp
// 只注销某个通知
NotificationCenter.DefaultCenter().RemoveObserver(this, NotificationNames.UserLogin);

// 注销该观察者的全部通知
NotificationCenter.DefaultCenter().RemoveObserver(this);

// 移除全部观察者
NotificationCenter.DefaultCenter().RemoveAllObservers();
```

## 核心 API

| API | 说明 |
| --- | --- |
| `NotificationCenter.DefaultCenter()` | 获取全局唯一通知中心 |
| `AddObserver(observer, name, callback)` | 注册观察者；`name` 支持 `string` 或 `NotificationName` |
| `Post(name, userInfo)` | 派发通知，回调在调用线程执行 |
| `RemoveObserver(observer, name)` | 按名称移除该观察者的通知 |
| `RemoveObserver(observer)` | 移除该观察者的全部通知 |
| `RemoveAllObservers()` | 移除所有观察者 |
| `Dispose()` | 等价于 `RemoveAllObservers()`，之后仍可继续使用 |
| `CallbackError` | 回调抛出异常时触发的事件 |

### `Notification`

| 成员 | 类型 | 说明 |
| --- | --- | --- |
| `Name` | `string` | 通知名称 |
| `UserInfo` | `object?` | 通知数据 |
| `ToString()` | `string` | 无数据返回名称，有数据返回 `名称 (数据)` |

### `NotificationErrorEventArgs`

| 属性 | 类型 | 说明 |
| --- | --- | --- |
| `Notification` | `Notification` | 正在派发的通知 |
| `Observer` | `object` | 回调所属的观察者 |
| `Exception` | `Exception` | 回调抛出的异常 |

### 行为约定

- 通知名按 **Ordinal** 比较，区分大小写
- 观察者按 **引用相等** 识别，不依赖 `Equals` 重写
- 同一观察者对同名通知重复注册时，最后一次注册覆盖前一次
- 派发使用观察者快照：派发过程中新增的观察者不会收到本次通知
- 回调执行顺序当前与注册顺序一致

## 线程模型与 UI

- `Post` 在调用方线程执行，回调也在同一线程执行
- `AddObserver`、`RemoveObserver`、`Post` 可从任意线程调用
- 回调在锁外执行，回调内部可以安全地再次注册或注销
- 后台线程派发时，如果需要更新 WPF/WinForms 控件，请自行切回 UI 线程

```csharp
NotificationCenter.DefaultCenter().AddObserver(this, "data.loaded", notification =>
{
    Application.Current?.Dispatcher.Invoke(() =>
    {
        StatusText.Text = notification.UserInfo?.ToString();
    });
});
```

## 异常处理

单个回调抛出的异常会被隔离，其他观察者仍会收到通知：

```csharp
NotificationCenter.DefaultCenter().CallbackError += (sender, e) =>
{
    Console.Error.WriteLine(
        $"[{e.Notification.Name}] {e.Observer.GetType().FullName}: {e.Exception}");
};
```

如果未订阅 `CallbackError`，异常信息会写入 `Trace`，不会向外抛出。事件处理器自身抛出的异常也会被捕获，确保派发流程不中断。

## 生命周期与内存

`NotificationCenter` 持有观察者的**强引用**。不再使用时请注销：

```csharp
protected override void OnClosed(EventArgs e)
{
    NotificationCenter.DefaultCenter().RemoveObserver(this);
    base.OnClosed(e);
}
```

> 弱引用模式无法解决回调闭包捕获 `this` 的问题，因此本项目不提供，仍建议显式注销。

## 示例项目

`NotificationSample` 是一个 WPF 示例，演示：

- UI 线程与后台线程派发通知
- 按名称取消订阅与重新订阅
- 一个回调抛异常时其他观察者仍能收到通知
- 同一观察者重复注册同名通知时的覆盖行为
- 通过日志中的线程号观察回调执行线程

示例源码见 [NotificationSample](https://github.com/gmi91dev/GMNotification/tree/main/NotificationSample)。

## 文档

完整 API 说明与开发文档见 [Docs/PROJECT.md](https://github.com/gmi91dev/GMNotification/blob/main/Docs/PROJECT.md)。

## 许可证

本项目基于 [MIT License](https://github.com/gmi91dev/GMNotification/blob/main/LICENSE) 发布。

Copyright (c) 2025 Gmi91
