using System;

namespace WorldIsMine.Net.Config
{
    [Serializable]
    public sealed class NetworkConfig
    {
        public string Host = "127.0.0.1";
        public int Port = 30017;
        public float ConnectTimeoutSeconds = 10f;
        public float RequestTimeoutSeconds = 10f;
        public float HeartbeatIntervalSeconds = 20f;

        public TimeSpan ConnectTimeout => TimeSpan.FromSeconds(Math.Max(1f, ConnectTimeoutSeconds));
        public TimeSpan RequestTimeout => TimeSpan.FromSeconds(Math.Max(1f, RequestTimeoutSeconds));
        public TimeSpan HeartbeatInterval => TimeSpan.FromSeconds(Math.Max(1f, HeartbeatIntervalSeconds));

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Host))
                throw new InvalidOperationException("Network host is required.");

            if (Port <= 0 || Port > 65535)
                throw new InvalidOperationException($"Network port is invalid: {Port}.");
        }
    }
}
