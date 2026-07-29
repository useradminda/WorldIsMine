using System;
using NUnit.Framework;
using WorldIsMine.Net.Protocol;

namespace WorldIsMine.Net.Tests
{
    public sealed class PacketCodecTests
    {
        [Test]
        public void EncodeAndRead_RoundTripsHeaderAndBody()
        {
            byte[] body = { 1, 2, 3, 4 };
            byte[] frame = PacketCodec.Encode(RequestCode.User, ActionCode.Login, 42, body);
            var reader = new PacketReader();

            reader.Append(frame, 0, frame.Length);

            Assert.AreEqual(PacketReadStatus.Success, reader.TryRead(out NetPacket packet));
            Assert.AreEqual(RequestCode.User, packet.RequestCode);
            Assert.AreEqual(ActionCode.Login, packet.ActionCode);
            Assert.AreEqual(42, packet.MessageId);
            CollectionAssert.AreEqual(body, packet.Body);
            Assert.AreEqual(0, reader.BufferedLength);
        }

        [Test]
        public void Reader_HandlesEveryFragmentBoundary()
        {
            byte[] frame = PacketCodec.Encode(RequestCode.Beat, ActionCode.Beat, 7, Array.Empty<byte>());

            for (int split = 1; split < frame.Length; split++)
            {
                var reader = new PacketReader();
                reader.Append(frame, 0, split);
                Assert.AreEqual(PacketReadStatus.NeedMoreData, reader.TryRead(out _), $"split={split}");
                reader.Append(frame, split, frame.Length - split);
                Assert.AreEqual(PacketReadStatus.Success, reader.TryRead(out NetPacket packet), $"split={split}");
                Assert.AreEqual(7, packet.MessageId);
            }
        }

        [Test]
        public void Reader_HandlesMultipleFramesInOneRead()
        {
            byte[] first = PacketCodec.Encode(RequestCode.Beat, ActionCode.Beat, 1, Array.Empty<byte>());
            byte[] second = PacketCodec.Encode(RequestCode.User, ActionCode.Login, 2, new byte[] { 9 });
            byte[] joined = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, joined, 0, first.Length);
            Buffer.BlockCopy(second, 0, joined, first.Length, second.Length);
            var reader = new PacketReader();

            reader.Append(joined, 0, joined.Length);

            Assert.AreEqual(PacketReadStatus.Success, reader.TryRead(out NetPacket firstPacket));
            Assert.AreEqual(1, firstPacket.MessageId);
            Assert.AreEqual(PacketReadStatus.Success, reader.TryRead(out NetPacket secondPacket));
            Assert.AreEqual(2, secondPacket.MessageId);
            Assert.AreEqual(PacketReadStatus.NeedMoreData, reader.TryRead(out _));
        }

        [Test]
        public void Reader_RejectsLengthSmallerThanFixedHeader()
        {
            byte[] invalid = { 15, 0, 0, 0 };
            var reader = new PacketReader();
            reader.Append(invalid, 0, invalid.Length);

            Assert.AreEqual(PacketReadStatus.InvalidPacket, reader.TryRead(out _));
            Assert.AreEqual(0, reader.BufferedLength);
        }
    }
}
