using System;

namespace WorldIsMine.Net.Protocol
{
    public readonly struct NetPacket
    {
        public NetPacket(RequestCode requestCode, ActionCode actionCode, long messageId, byte[] body)
        {
            RequestCode = requestCode;
            ActionCode = actionCode;
            MessageId = messageId;
            Body = body ?? Array.Empty<byte>();
        }

        public RequestCode RequestCode { get; }
        public ActionCode ActionCode { get; }
        public long MessageId { get; }
        public byte[] Body { get; }
    }
}
