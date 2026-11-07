namespace NotificationSample.Models
{
    /// <summary>
    /// 请求成功时携带的负载示例。
    /// </summary>
    public sealed record UserPayload(string Name, int Age);
}
