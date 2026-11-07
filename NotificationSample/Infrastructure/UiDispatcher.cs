using System.Windows.Threading;

namespace NotificationSample.Infrastructure
{
    /// <summary>
    /// 将回调调度到 WPF UI 线程，视图模型无需直接依赖 Application.Current。
    /// </summary>
    public sealed class UiDispatcher
    {
        private readonly Dispatcher _dispatcher;

        public UiDispatcher(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public void Invoke(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (_dispatcher.CheckAccess())
            {
                action();
                return;
            }

            _dispatcher.Invoke(action);
        }
    }
}
