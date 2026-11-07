using GMNotification;
using NotificationSample.Notifications;

namespace NotificationSample.Services
{
    /// <summary>
    /// 模拟一次请求：先派发 started，等待一段时间后派发结果通知。
    /// 视图模型只负责组织流程，不直接调用 NotificationCenter。
    /// </summary>
    public sealed class RequestService
    {
        private readonly NotificationCenter _center;
        private readonly TimeSpan _latency;

        public RequestService(NotificationCenter center, TimeSpan? latency = null)
        {
            _center = center ?? throw new ArgumentNullException(nameof(center));
            _latency = latency ?? TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// 在调用方线程（通常是 UI 线程）派发通知。
        /// </summary>
        public Task SimulateAsync(NotificationName resultName, object? data)
            => SimulateCoreAsync(resultName, data, useBackgroundThread: false);

        /// <summary>
        /// 在后台线程派发通知，用于观察回调所在线程。
        /// </summary>
        public Task SimulateOnBackgroundThreadAsync(NotificationName resultName, object? data)
            => SimulateCoreAsync(resultName, data, useBackgroundThread: true);

        private async Task SimulateCoreAsync(NotificationName resultName, object? data, bool useBackgroundThread)
        {
            if (useBackgroundThread)
            {
                await Task.Run(async () =>
                {
                    _center.Post(NotificationNames.RequestStarted);
                    await Task.Delay(_latency);
                    _center.Post(resultName, data);
                });

                return;
            }

            _center.Post(NotificationNames.RequestStarted);
            await Task.Delay(_latency);
            _center.Post(resultName, data);
        }
    }
}
