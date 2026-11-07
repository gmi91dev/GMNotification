using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using GMNotification;
using NotificationSample.Infrastructure;
using NotificationSample.Models;
using NotificationSample.Notifications;
using NotificationSample.Services;

namespace NotificationSample.ViewModels
{
    /// <summary>
    /// 主窗口的状态与交互逻辑。视图只负责转发事件和处理纯 UI 行为。
    /// </summary>
    public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly NotificationCenter _center;
        private readonly UiDispatcher _ui;
        private readonly RequestService _requestService;
        private readonly NotificationDemoService _demoService;

        private string _status = "就绪";
        private string _successSubscriptionButtonText = "取消订阅：成功通知";
        private bool _isSuccessSubscribed = true;
        private bool _disposed;

        public MainWindowViewModel(NotificationCenter center, Dispatcher dispatcher)
        {
            _center = center ?? throw new ArgumentNullException(nameof(center));
            _ui = new UiDispatcher(dispatcher);
            _requestService = new RequestService(center);
            _demoService = new NotificationDemoService(center, AppendLog);

            _center.CallbackError += OnCallbackError;
            _center.AddObserver(this, NotificationNames.RequestStarted, OnRequestStarted);
            _center.AddObserver(this, NotificationNames.RequestSucceeded, OnRequestSucceeded);
            _center.AddObserver(this, NotificationNames.RequestFailed, OnRequestFailed);

            AppendLog($"已注册 3 个通知：{NotificationNames.RequestStarted}、{NotificationNames.RequestSucceeded}、{NotificationNames.RequestFailed}");
            AppendLog("另注册了异常隔离与同名覆盖演示，可点击对应按钮观察行为");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<string> LogEntries { get; } = new();

        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        public string SuccessSubscriptionButtonText
        {
            get => _successSubscriptionButtonText;
            private set => SetProperty(ref _successSubscriptionButtonText, value);
        }

        public async Task RunSuccessRequestAsync()
        {
            AppendLog($"在 UI 线程发起请求，稍后派发 {NotificationNames.RequestSucceeded}");
            await _requestService.SimulateAsync(NotificationNames.RequestSucceeded, new UserPayload("张三", 28));
        }

        public async Task RunFailureRequestAsync()
        {
            AppendLog($"在 UI 线程发起请求，稍后派发 {NotificationNames.RequestFailed}");
            await _requestService.SimulateAsync(NotificationNames.RequestFailed, "网络连接超时（模拟）");
        }

        public async Task RunBackgroundRequestAsync()
        {
            AppendLog("在后台线程发起请求，注意日志中的线程号变化");
            await _requestService.SimulateOnBackgroundThreadAsync(NotificationNames.RequestFailed, "后台任务失败（模拟）");
        }

        public void ToggleSuccessSubscription()
        {
            if (_isSuccessSubscribed)
            {
                _center.RemoveObserver(this, NotificationNames.RequestSucceeded);
                _isSuccessSubscribed = false;
                SuccessSubscriptionButtonText = "重新订阅：成功通知";
                AppendLog($"已注销 {NotificationNames.RequestSucceeded}，之后的成功通知不会再触发回调");
                return;
            }

            _center.AddObserver(this, NotificationNames.RequestSucceeded, OnRequestSucceeded);
            _isSuccessSubscribed = true;
            SuccessSubscriptionButtonText = "取消订阅：成功通知";
            AppendLog($"已重新注册 {NotificationNames.RequestSucceeded}");
        }

        public void TriggerCallbackError() => _demoService.TriggerCallbackError();

        public void TriggerOverrideDemo() => _demoService.TriggerOverride();

        public void ClearLog()
        {
            _ui.Invoke(() =>
            {
                LogEntries.Clear();
                Status = "就绪";
            });
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _center.CallbackError -= OnCallbackError;
            _center.RemoveObserver(this);
            _demoService.Dispose();
        }

        private void OnRequestStarted(Notification notification)
        {
            AppendLog($"收到 {notification.Name}");
            SetStatus("请求中…");
        }

        private void OnRequestSucceeded(Notification notification)
        {
            if (notification.UserInfo is UserPayload user)
            {
                AppendLog($"收到 {notification.Name}，负载：{user.Name} / {user.Age} 岁");
                SetStatus($"请求成功：{user.Name}");
                return;
            }

            AppendLog($"收到 {notification.Name}，但负载类型不匹配");
        }

        private void OnRequestFailed(Notification notification)
        {
            string reason = notification.UserInfo as string ?? "未知错误";
            AppendLog($"收到 {notification.Name}，负载：{reason}");
            SetStatus($"请求失败：{reason}");
        }

        private void OnCallbackError(object? sender, NotificationErrorEventArgs e)
        {
            AppendLog($"CallbackError：{e.Notification.Name} 的观察者抛出 {e.Exception.GetType().Name}：{e.Exception.Message}");
            SetStatus("回调异常已被隔离");
        }

        private void AppendLog(string message)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss.fff}] [线程 {Environment.CurrentManagedThreadId}] {message}";
            _ui.Invoke(() => LogEntries.Add(entry));
        }

        private void SetStatus(string value) => _ui.Invoke(() => Status = value);

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
