using System;

namespace WorldIsMine.Net.Protocol
{
    public enum PacketReadStatus
    {
        NeedMoreData,
        Success,
        InvalidPacket
    }

    public static class PacketCodec
    {
        public const int LengthFieldSize = 4;
        public const int FixedHeaderSize = 16;
        public const int MaxPacketSize = 2 * 1024 * 1024;

        public static byte[] Encode(
            RequestCode requestCode,
            ActionCode actionCode,
            long messageId,
            byte[] body)
        {
            body ??= Array.Empty<byte>();
            int totalLength = checked(FixedHeaderSize + body.Length);
            if (totalLength > MaxPacketSize)
                throw new InvalidOperationException($"Packet is too large: {totalLength}.");

            byte[] output = new byte[LengthFieldSize + totalLength];
            WriteInt32(output, 0, totalLength);
            WriteInt32(output, 4, (int)requestCode);
            WriteInt32(output, 8, (int)actionCode);
            WriteInt64(output, 12, messageId);
            if (body.Length > 0)
                Buffer.BlockCopy(body, 0, output, 20, body.Length);

            return output;
        }

        internal static int ReadInt32(byte[] source, int offset)
        {
            return source[offset]
                   | source[offset + 1] << 8
                   | source[offset + 2] << 16
                   | source[offset + 3] << 24;
        }

        internal static long ReadInt64(byte[] source, int offset)
        {
            uint low = unchecked((uint)ReadInt32(source, offset));
            uint high = unchecked((uint)ReadInt32(source, offset + 4));
            return unchecked((long)((ulong)low | (ulong)high << 32));
        }

        private static void WriteInt32(byte[] destination, int offset, int value)
        {
            destination[offset] = (byte)value;
            destination[offset + 1] = (byte)(value >> 8);
            destination[offset + 2] = (byte)(value >> 16);
            destination[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteInt64(byte[] destination, int offset, long value)
        {
            unchecked
            {
                WriteInt32(destination, offset, (int)value);
                WriteInt32(destination, offset + 4, (int)(value >> 32));
            }
        }
    }

    public sealed class PacketReader
    {
        private byte[] _buffer = new byte[64 * 1024];
        private int _length;

        public int BufferedLength => _length;

        public void Append(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || count < 0 || offset + count > data.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            EnsureCapacity(count);
            Buffer.BlockCopy(data, offset, _buffer, _length, count);
            _length += count;
        }

        public PacketReadStatus TryRead(out NetPacket packet)
        {
            packet = default;
            if (_length < PacketCodec.LengthFieldSize)
                return PacketReadStatus.NeedMoreData;

            int totalLength = PacketCodec.ReadInt32(_buffer, 0);
            if (totalLength < PacketCodec.FixedHeaderSize || totalLength > PacketCodec.MaxPacketSize)
            {
                _length = 0;
                return PacketReadStatus.InvalidPacket;
            }

            int wireLength = PacketCodec.LengthFieldSize + totalLength;
            if (_length < wireLength)
                return PacketReadStatus.NeedMoreData;

            var requestCode = (RequestCode)PacketCodec.ReadInt32(_buffer, 4);
            var actionCode = (ActionCode)PacketCodec.ReadInt32(_buffer, 8);
            long messageId = PacketCodec.ReadInt64(_buffer, 12);
            int bodyLength = totalLength - PacketCodec.FixedHeaderSize;
            byte[] body = bodyLength == 0 ? Array.Empty<byte>() : new byte[bodyLength];
            if (bodyLength > 0)
                Buffer.BlockCopy(_buffer, 20, body, 0, bodyLength);

            int remaining = _length - wireLength;
            if (remaining > 0)
                Buffer.BlockCopy(_buffer, wireLength, _buffer, 0, remaining);
            _length = remaining;

            packet = new NetPacket(requestCode, actionCode, messageId, body);
            return PacketReadStatus.Success;
        }

        public void Reset()
        {
            _length = 0;
        }

        private void EnsureCapacity(int appendLength)
        {
            int required = checked(_length + appendLength);
            int maximum = PacketCodec.LengthFieldSize + PacketCodec.MaxPacketSize;
            if (required > maximum)
                throw new InvalidOperationException($"Receive buffer exceeds {maximum} bytes.");
            if (required <= _buffer.Length)
                return;

            int size = _buffer.Length;
            while (size < required && size < maximum)
                size = Math.Min(size * 2, maximum);
            Array.Resize(ref _buffer, size);
        }
    }
}
