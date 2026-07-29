using System;

namespace WorldIsMine.Net.Services
{
    [Serializable]
    public sealed class PlayerLoginOptions
    {
        public string RoleId = string.Empty;
        public int RoleType = 1;
        public string AccountId = string.Empty;
        public string Platform = "dy";
        public string OpenId = string.Empty;
        public string Nickname = string.Empty;
        public string Avatar = string.Empty;
        public bool CreateIfMissing = true;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(RoleId))
                throw new InvalidOperationException("RoleId is required.");
            if (RoleType <= 0)
                throw new InvalidOperationException("RoleType must be greater than zero.");
        }
    }

    [Serializable]
    public sealed class BindOptions
    {
        public string AnchorId = string.Empty;
        public string AnchorName = string.Empty;
        public string Platform = "dy";
        public string RoomId = string.Empty;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(AnchorId))
                throw new InvalidOperationException("AnchorId is required.");
            if (string.IsNullOrWhiteSpace(RoomId))
                throw new InvalidOperationException("RoomId is required.");
        }
    }
}
