using System;

namespace WorldIsMine.Net.Services
{
    [Serializable]
    public sealed class BindOptions
    {
        public string AnchorId = string.Empty;
        public string AnchorName = string.Empty;
        public string Platform = "dy";
        public string RoomId = string.Empty;
        public string AuthTicket = string.Empty;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(AnchorId))
                throw new InvalidOperationException("AnchorId is required.");
            if (string.IsNullOrWhiteSpace(RoomId))
                throw new InvalidOperationException("RoomId is required.");
        }
    }
}
