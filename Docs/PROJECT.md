# GMNotification 项目文档

> 一个轻量级的 .NET 通知中心（Notification Center）实现，参考 iOS/macOS 中 `NSNotificationCenter` 的设计模式。

---

## 目录

- [1. 项目概述](#1-项目概述)
- [2. 解决方案结构](#2-解决方案结构)
- [3. 核心库 GMNotification](#3-核心库-gmnotification)
  - [3.1 Notification](#31-notification)
  - [3.2 NotificationObserver](#32-notificationobserver)
  - [3.3 NotificationCenter](#33-notificationcenter)
  - [3.4 NotificationName](#34-notificationname)
- [4. 示例项目 NotificationSample](#4-示例项目-notificationsample)
- [5. 架构与设计](#5-架构与设计)
- [6. API 使用说明](#6-api-使用说明)
- [7. 编译与运行](#7-编译与运行)
- [8. 已知问题与改进建议](#8-已知问题与改进建议)
- [9. 发布到 NuGet](#9-发布到-nuget)
- [10. 许可证](#10-许可证)

---

## 1. 项目概述

**GMNotification** 是一个面向 .NET 8 的轻量级通知中心库。核心库不依赖 WPF，可在控制台、WPF、WinForms、ASP.NET Core 等任意 .NET 8 应用中复用；示例项目使用 WPF 展示 UI 线程与后台线程下的派发行为。

| 属性 | 值 |
| --- | --- |
| 项目名称 | GMNotification |
| 目标框架 | `net8.0`（核心库） / `net8.0-windows`（WPF 示例） |
| 命名空间 | `GMNotification` |
| 设计模式 | 观察者模式（Observer Pattern）+ 单例（Singleton） |
| 许可证 | MIT（Copyright (c) 2025 Gmi91） |

主要特性：

- **单例中心**：全局唯一的 `NotificationCenter`，通过 `Lazy<T>`（`ExecutionAndPublication`）保证线程安全的延迟初始化。
- **基于名称（Name）派发**：发送方通过通知名称标识通知类型，既可使用字符串，也可使用类型安全的 `NotificationName`。
- **对象 + 通知绑定**：观察者按「宿主对象 + 通知名称」注册，回调在派发线程上执行。
- **支持数据（UserInfo）**：可附带任意 `object` 作为通知数据，回调接收时使用；`Notification` 不可变，可在多个回调间共享。
- **重复注册覆盖**：同一观察者对同一通知名重复注册时，最后一次注册覆盖前一次，底层使用字典实现 O(1) 查找。
- **线程安全**：注册、注销、派发均由锁保护；派发在快照上、锁外执行，回调内可安全地再次注册/注销。
- **异常隔离**：单个回调抛出的异常不会中断其余观察者，可通过 `CallbackError` 事件统一观测。
- **IDisposable**：`NotificationCenter` 实现 `IDisposable`，可一键清空所有观察者。

---

## 2. 解决方案结构

```
GMNotification/
├── GMNotification.sln             # 解决方案文件
├── LICENSE                         # MIT 许可证
├── README.md                       # 项目说明与快速开始
├── .gitignore                      # 标准 Visual Studio 忽略规则
├── GMNotification/                 # 核心库（class library）
│   ├── GMNotification.csproj
│   ├── Notification.cs             # 通知数据封装（不可变）
│   ├── NotificationName.cs         # 类型安全的通知名
│   ├── NotificationObserver.cs     # 观察者内部封装（名称 -> 回调字典）
│   ├── NotificationErrorEventArgs.cs # 回调异常事件参数
│   └── NotificationCenter.cs       # 单例通知中心
├── GMNotification.Tests/           # xUnit 单元测试（net8.0）
│   ├── GMNotification.Tests.csproj
│   ├── TestHelpers.cs              # 测试基类 / CallbackError 订阅辅助
│   ├── NotificationCenterTests.cs  # 注册/派发/注销/重入
│   ├── NotificationCenterExceptionTests.cs # 异常隔离与 CallbackError
│   ├── NotificationCenterConcurrencyTests.cs # 并发场景
│   ├── NotificationObserverTests.cs # 内部观察者
│   ├── NotificationNameTests.cs    # 类型安全的通知名
│   └── NotificationTests.cs        # Notification 数据对象
└── NotificationSample/             # WPF 示例项目（WinExe）
    ├── NotificationSample.csproj
    ├── App.xaml / App.xaml.cs
    ├── AssemblyInfo.cs
    ├── MainWindow.xaml / MainWindow.xaml.cs   # 仅 UI 事件转发与滚动定位
    ├── Infrastructure/
    │   └── UiDispatcher.cs          # 切换回 UI 线程
    ├── Models/
    │   └── UserData.cs               # 成功请求数据
    ├── Notifications/
    │   └── NotificationNames.cs     # 集中声明的通知名
    ├── Services/
    │   ├── RequestService.cs        # 模拟请求并派发通知
    │   └── NotificationDemoService.cs # 异常隔离/同名覆盖演示
    └── ViewModels/
        └── MainWindowViewModel.cs   # 状态、日志与交互逻辑
```

工程引用关系：

```
NotificationSample ──► GMNotification
GMNotification.Tests ─► GMNotification
```

---

## 3. 核心库 GMNotification

### 3.1 Notification

文件：`GMNotification/Notification.cs`

通知的最小数据单元，对应一次「发出的消息」。

| 成员 | 类型 | 说明 |
| --- | --- | --- |
| `Name` | `string`（只读） | 通知名称，作为派发的标识 |
| `UserInfo` | `object?`（只读） | 通知携带的可选数据 |
| `ToString()` | `string` | 便于调试：无数据时返回名称，否则返回 `名称 (数据)` |

构造方式（`internal`，仅供库内部使用）：

```csharp
internal Notification(string name, object? userInfo = null);
```

设计要点：

- `Notification` 是**纯数据对象**：只保存 `Name` 和 `UserInfo`，不再持有回调。
- 实例**不可变**，`Post` 时构造一次即可被所有观察者共享，避免重复分配。
- 回调统一保存在 `NotificationObserver` 内部，注册与派发职责分离。

---

### 3.2 NotificationObserver

文件：`GMNotification/NotificationObserver.cs`

> 类型修饰符：`internal`，对库外不可见。

封装单个宿主对象所注册的全部通知，内部使用 `Dictionary<string, Action<Notification>>` 保存「通知名 → 回调」，并带独立的锁。

| 成员 | 说明 |
| --- | --- |
| `Observer` | 注册时传入的宿主对象，用于在移除时定位 |
| `_callbacks` | 该观察者注册的所有「通知名 → 回调」 |
| `AddNotification(name, callback)` | 写入回调；同名时直接覆盖（字典索引器赋值） |
| `RemoveNotification(name)` | 仅移除指定名称的通知 |
| `GetCallback(name)` | 返回匹配的回调，未注册时返回 `null` |
| `IsEmpty` | 是否已没有任何注册 |

关键行为：

- **同名覆盖**：`_callbacks[name] = callback` 天然实现最后一次注册覆盖前一次。
- **O(1) 查找**：派发时无需遍历列表；名称比较使用 `StringComparer.Ordinal`，大小写敏感且不受区域设置影响。
- **锁外执行**：`GetCallback` 在锁内置取回调，实际回调在锁外执行，回调内可安全地再次注册或注销。

---

### 3.3 NotificationCenter

文件：`GMNotification/NotificationCenter.cs`

通知中心对外门面，单例。

#### 3.3.1 单例

```csharp
public static NotificationCenter DefaultCenter();
```

通过 `Lazy<NotificationCenter>` + `LazyThreadSafetyMode.ExecutionAndPublication` 保证线程安全，仅在首次访问时创建实例。

#### 3.3.2 注册观察者

```csharp
public void AddObserver(object observer, string name, Action<Notification> callbackAction);
```

- `observer`：宿主对象。**用于在移除时识别归属**，通常是 `this`（UI 控件）或某个业务对象。
- `name`：通知名称。
- `callbackAction`：回调函数，签名 `void(Notification noti)`。

逻辑：

1. 通过 `ReferenceEquals` 查找观察者（同一实例才会被视为同一观察者）。
2. 若观察者已存在，把「通知名 → 回调」写入其字典。
3. 否则创建新的 `NotificationObserver` 并加入 `_registeredObservers`。
4. 同名通知将被覆盖（最终只保留最后一次回调）。

参数在进入锁之前完成校验：`observer`、`callbackAction` 不能为 `null`，`name` 不能为 `null` 或空字符串。

#### 3.3.3 发送通知

```csharp
public void Post(string name, object? userInfo = null);
```

- 在锁内复制一份观察者快照，锁外依次派发；
- 构造**一个**不可变的 `Notification` 实例，供所有回调共享；
- 没有匹配时直接返回（静默忽略）；
- 每个回调独立 `try/catch`：单个观察者抛异常不会中断其余观察者，异常通过 `CallbackError` 事件上报。

> 注意：`Post` 默认在调用方线程执行，回调线程由调用方决定。WPF 示例中回调通过 `Dispatcher` 切回 UI 线程。

#### 3.3.4 移除观察者

```csharp
public void RemoveObserver(object observer, string notificationName = "");
```

- `notificationName` 为空时：移除该观察者的**所有**通知；
- `notificationName` 非空时：仅移除指定名称的通知。

#### 3.3.5 全部清空与释放

```csharp
public void RemoveAllObservers();
public void Dispose();   // 等价于 RemoveAllObservers()，之后仍可继续使用
```

#### 3.3.6 回调异常事件

```csharp
public event EventHandler<NotificationErrorEventArgs>? CallbackError;
```

当某个观察者的回调抛出异常时触发，事件参数包含：

| 属性 | 类型 | 说明 |
| --- | --- | --- |
| `Notification` | `Notification` | 正在派发的通知 |
| `Observer` | `object` | 回调所属的观察者对象 |
| `Exception` | `Exception` | 回调抛出的异常 |

若没有订阅该事件，异常会写入 `Trace`，不会向外抛出；事件处理器自身抛出的异常也会被捕获并写入 `Trace`，确保派发流程不中断。

```csharp
NotificationCenter.DefaultCenter().CallbackError += (sender, e) =>
{
	Console.Error.WriteLine($"[{e.Notification.Name}] {e.Exception.Message}");
};
```

---

### 3.4 NotificationName

文件：`GMNotification/NotificationName.cs`

类型安全的通知名，用于替代散落的裸字符串。它是 `readonly struct`，可与 `string` 隐式互转，因此现有使用字符串的代码无需修改。

```csharp
public readonly struct NotificationName : IEquatable<NotificationName>
{
	public NotificationName(string value);
	public string Value { get; }
	public static implicit operator NotificationName(string value);
	public static implicit operator string(NotificationName name);
}
```

使用方式：

```csharp
public static class NotificationNames
{
	public static readonly NotificationName UserLogin = new("user.login");
}

NotificationCenter.DefaultCenter().AddObserver(this, NotificationNames.UserLogin, OnUserLogin);
var userInfo = new User("Alice");
NotificationCenter.DefaultCenter().Post(NotificationNames.UserLogin, userInfo);
NotificationCenter.DefaultCenter().RemoveObserver(this, NotificationNames.UserLogin);
```

设计要点：

- 构造时校验 `null`/空字符串，提前暴露错误。
- 相等性使用 `StringComparison.Ordinal`，大小写敏感、不受区域设置影响。
- `default(NotificationName)` 的 `ToString()` 返回空字符串，不会抛异常；但不应作为有效通知名使用。
- 字符串重载仍然保留，新旧 API 可以混用。

---

## 4. 示例项目 NotificationSample

文件：`NotificationSample/MainWindow.xaml.cs`

示例采用「View + ViewModel + Service」分层：`MainWindow.xaml.cs` 只负责 UI 事件转发和滚动定位，通知订阅、日志、状态与演示逻辑分别由 `MainWindowViewModel` 和 `Services` 承担。

### 4.1 演示场景

示例用 `NotificationNames` 集中声明 `NotificationName` 类型的通知名，用 `UserData` 作为成功请求的强类型数据。

| 按钮 | 演示内容 |
| --- | --- |
| 发起请求（成功） | UI 线程派发 `request.started`，1 秒后派发 `request.succeeded`，数据为 `UserData` |
| 发起请求（失败） | UI 线程派发 `request.started`，1 秒后派发 `request.failed`，数据为错误字符串 |
| 后台线程发起请求 | 在 `Task.Run` 中派发通知，日志里的线程号会变化，用于观察回调线程 |
| 取消订阅 / 重新订阅 | `RemoveObserver(this, name)` 与 `AddObserver` 的按名称注销/注册 |
| 触发回调异常 | 一个观察者抛异常，另一个观察者仍能收到通知，并通过 `CallbackError` 记录异常 |
| 演示同名覆盖 | 同一观察者对同名通知注册两次，只有最后一次注册的回调被触发 |
| 清空日志 | 清空 `ListBox` 并重置状态栏 |

窗口关闭时 `MainWindow.OnClosed` 调用 `MainWindowViewModel.Dispose()`，统一注销视图模型与演示服务的全部订阅；应用退出时 `App.OnExit` 调用 `NotificationCenter.DefaultCenter().Dispose()`。

### 4.2 回调中更新 UI

视图模型通过 `UiDispatcher` 统一切回 UI 线程：

```csharp
private void AppendLog(string message)
{
    string entry = $"[{DateTime.Now:HH:mm:ss.fff}] [线程 {Environment.CurrentManagedThreadId}] {message}";
    _ui.Invoke(() => LogEntries.Add(entry));
}
```

`UiDispatcher.Invoke` 在 UI 线程上直接执行，在后台线程上调用 `Dispatcher.Invoke`，因此视图模型不需要直接依赖 `Application.Current`。

### 4.3 UserInfo 传递

```csharp
var data = new UserData("张三", 28);
NotificationCenter.DefaultCenter().Post(NotificationNames.RequestSucceeded, data);
```

回调中通过 `notification.UserInfo is UserData user` 还原数据，避免使用字符串字典带来的运行时类型错误。

---

## 5. 架构与设计

### 5.1 派发流程

```
Poster ──Post(name, userInfo)──► NotificationCenter
                                   │
                                   ▼
                    锁内复制 _registeredObservers 快照
                                   │
                                   ▼
                    构造一个不可变 Notification(name, userInfo)
                                   │
                     ┌─────────────┴─────────────┐
                     ▼                           ▼
            NotificationObserver          NotificationObserver
                     │ GetCallback(name)         │
                     ▼                           ▼
              Callback(notification)        ...
                     │
        try/catch ◄──┴──► CallbackError 事件 / Trace
```

### 5.2 关键设计决策

| 决策 | 说明 |
| --- | --- |
| 字符串作为通知名 | 简单、可跨程序集使用；示例通过常量类集中管理，降低拼写错误 |
| 单例中心 | 全局统一入口，类似 `NSNotificationCenter.defaultCenter` |
| `Lazy<T>` 单例 | 延迟初始化且线程安全，无双重检查锁的样板代码 |
| 观察者内用字典保存回调 | 同名覆盖与按名查找均为 O(1) |
| 派发前复制快照 | 回调内注册/注销不会破坏当前遍历 |
| 共享不可变 `Notification` | 一次派发只分配一个实例，回调也无法篡改派发数据 |
| 单观察者异常隔离 | 一个回调失败不影响其他观察者，异常通过事件上报 |
| `IDisposable` | 显式释放语义，便于在容器/作用域结束时清理 |

### 5.3 线程模型

- 注册、注销、派发均由 `NotificationCenter` 内部锁保护，可从任意线程调用。
- 发送（`Post`）发生在调用线程；回调同样在调用线程执行。
- 派发使用快照并在锁外执行回调，回调内部可以安全地再次注册或注销；同一观察者的一次派发最多执行一次对应回调。
- 若需要在后台线程接收并访问 UI，必须在回调内部自行调度到 UI 线程（见示例）。
- `NotificationCenter` 持有观察者的强引用，不再使用时需要调用 `RemoveObserver`（或 `Dispose`），否则会产生泄漏。

---

## 6. API 使用说明

### 6.1 最小示例

```csharp
// 集中声明（推荐）
public static class NotificationNames
{
	public static readonly NotificationName UserLogin = new("user.login");
}

// 注册
NotificationCenter.DefaultCenter().AddObserver(this, NotificationNames.UserLogin, noti =>
{
	var user = noti.UserInfo as User;
	Console.WriteLine($"{noti.Name}: {user?.Name}");
});

// 发送
var data = new User { Name = "Alice" };
NotificationCenter.DefaultCenter().Post(NotificationNames.UserLogin, data);

// 移除（按名称）
NotificationCenter.DefaultCenter().RemoveObserver(this, NotificationNames.UserLogin);

// 移除该对象全部
NotificationCenter.DefaultCenter().RemoveObserver(this);

// 全部清空
NotificationCenter.DefaultCenter().RemoveAllObservers();
```

字符串重载仍然可用，适合不需要集中管理通知名的简单场景：

```csharp
NotificationCenter.DefaultCenter().AddObserver(this, "user.login", OnUserLogin);
NotificationCenter.DefaultCenter().Post("user.login", new User { Name = "Alice" });
```

### 6.2 在 WPF 中跨线程安全更新

```csharp
NotificationCenter.DefaultCenter().AddObserver(this, "data.loaded", noti =>
{
	Application.Current?.Dispatcher.Invoke(() =>
	{
		// 在这里安全访问 UI 元素
		StatusText.Text = noti.UserInfo?.ToString();
	});
});
```

### 6.3 在应用退出时释放

```csharp
protected override void OnExit(ExitEventArgs e)
{
	NotificationCenter.DefaultCenter().Dispose();
	base.OnExit(e);
}
```

### 6.4 观测回调异常

```csharp
NotificationCenter.DefaultCenter().CallbackError += (sender, e) =>
{
	logger.LogError(
		e.Exception,
		"通知 {Name} 的观察者 {Observer} 回调执行失败",
		e.Notification.Name,
		e.Observer.GetType().FullName);
};
```

异常隔离只保证「不影响其他观察者」，生产环境仍应订阅 `CallbackError` 并记录日志，否则问题只能从 `Trace` 中找到。

---

## 7. 编译与运行

### 环境要求

- .NET SDK 8.0 或更高
- 核心库可在任何支持 .NET 8 的平台运行
- 运行 `NotificationSample` 需要 Windows（WPF）

### 命令

在仓库根目录：

```bash
# 还原并构建整个解决方案
dotnet build GMNotification.sln

# 仅构建库
dotnet build GMNotification/GMNotification.csproj

# 运行测试
dotnet test GMNotification.sln

# 运行测试并生成覆盖率报告（输出到 TestResults/）
dotnet test GMNotification.sln --collect:"XPlat Code Coverage" --results-directory TestResults

# 运行示例
dotnet run --project NotificationSample/NotificationSample.csproj
```

或直接使用 Visual Studio 2022/2026 打开 `GMNotification.sln`。测试项目使用 xUnit，共 90 个测试，覆盖注册/派发/注销、同名覆盖、回调异常隔离、并发、`NotificationName`、`Notification` 数据对象以及内部 `NotificationObserver` 等场景。使用 coverlet 收集覆盖率，当前核心库行覆盖率和分支覆盖率均为 100%。

仓库包含 GitHub Actions 工作流（`.github/workflows/ci.yml`），在 push 到 `main` 或提交 PR 时自动执行 `dotnet build` 与 `dotnet test`。

---

## 8. 已知问题与改进建议

| 类型 | 位置 | 描述 | 建议 |
| --- | --- | --- | --- |
| 强引用 | `NotificationCenter` | 中心持有观察者的强引用，忘记注销会导致泄漏 | 在 `OnClosed`/`Dispose` 中注销；弱引用模式无法解决回调闭包捕获 `this` 的问题，需谨慎引入 |
| 观察者查找 | `NotificationCenter.FindObserver` | 按引用线性查找，O(n)，但保留注册顺序 | 观察者数量通常很少；若数量很大可换用引用比较的字典 |
| 分发顺序 | 公共 API | 目前未承诺注册顺序 | 如业务依赖顺序，在文档中明确或提供有序选项 |

---

## 9. 发布到 NuGet

项目使用 nuget.org 的 **Trusted Publishing**（OIDC）发布，不再保存长期 API Key。工作流通过 GitHub OIDC 令牌向 nuget.org 换取有效期为 **1 小时**的临时 API Key。

官方文档：<https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing>

### 9.1 前置条件

- nuget.org 账号
- 包 ID `GMNotification` 当前未被占用
- 仓库已推送到 GitHub

### 9.2 配置 Trusted Publishing 策略

1. 登录 nuget.org，点击用户名，选择 **Trusted Publishing**
2. 添加新策略：
   - **Repository Owner**：`gmi91dev`
   - **Repository**：`GMNotification`
   - **Workflow File**：`release.yml`（只填文件名，不含 `.github/workflows/`）
   - **Environment**：留空（工作流未使用 GitHub Environments）
3. 配置 Policy Scopes：
   - 允许发布新包和新版本
   - Glob pattern：`GMNotification`

### 9.3 配置 GitHub Secret（`NUGET_USER`）

> 不要点左侧的 **Actions**（那是 Actions 的通用设置）。要找 **Secrets and variables → Actions**。

1. 用仓库 owner/admin 账号登录 GitHub，打开：
   <https://github.com/gmi91dev/GMNotification/settings/secrets/actions>
2. 页面顶部选择 **Secrets** 标签（不是 Variables）。
3. 在 **Repository secrets** 区域点击 **New repository secret**。
4. 填写：
   - **Name**：`NUGET_USER`（大小写一致）
   - **Secret**：nuget.org 的**用户名**（个人资料名，不是邮箱；例如个人主页为 `https://www.nuget.org/profiles/gmi91`，则填 `gmi91`）
5. 点击 **Add secret**。

不要使用以下选项：

- **Environment secrets**：工作流没有声明 `environment`
- **Variables**：这是非敏感变量，不是 Secret
- **Organization/Dependabot secrets**：与当前工作流无关

也可以使用 GitHub CLI：

```bash
gh secret set NUGET_USER --repo gmi91dev/GMNotification
# 按提示输入 nuget.org 用户名
gh secret list --repo gmi91dev/GMNotification
```

不需要配置长期有效的 API Key Secret；工作流使用的是 `NuGet/login` 输出的临时 `NUGET_API_KEY`。

### 9.4 发布

```bash
git tag v1.0.0
git push origin v1.0.0
```

`.github/workflows/release.yml` 会自动：

1. 从标签解析版本（`v1.0.0` → `1.0.0`）
2. 执行 restore / build / test / pack
3. 通过 `NuGet/login@v1` 使用 GitHub OIDC 令牌换取临时 API Key
4. 推送 `.nupkg` 与 `.snupkg` 到 nuget.org
5. 创建 GitHub Release

也可以在 GitHub Actions 页面手动触发 **Release** 工作流并输入版本号（不创建 GitHub Release）。

### 9.5 注意事项

- 临时 API Key 只能换取一次且有效期 1 小时，工作流已将其放在打包之后、推送之前获取
- 工作流已声明 `permissions: id-token: write`，这是通过 OIDC 换取临时 API Key 的必要条件
- 如果 Trusted Publishing 策略配置了 Environment，工作流必须声明相同的 `environment`
- 使用 SemVer 标签：`v1.0.0`、`v1.1.0`、`v1.1.0-beta.1`
- 相同版本重复推送会被 nuget.org 拒绝，工作流使用 `--skip-duplicate` 避免失败
- 包 ID 首次发布后不可修改

---

## 10. 许可证

本项目基于 **MIT License** 发布，详情参见 [LICENSE](../LICENSE)。

```
MIT License
Copyright (c) 2025 Gmi91
```