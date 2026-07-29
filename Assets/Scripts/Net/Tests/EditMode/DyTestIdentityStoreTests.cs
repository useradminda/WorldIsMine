using System;
using System.IO;
using NUnit.Framework;
using WorldIsMine.Net.Protocol;
using WorldIsMine.Net.Services;

namespace WorldIsMine.Net.Tests
{
    public sealed class DyTestIdentityStoreTests
    {
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(
                Path.GetTempPath(),
                $"worldismine-dy-test-{Guid.NewGuid():N}.md");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }

        [Test]
        public void SaveAndLoad_RoundTripsAnchorAndRoom()
        {
            DyTestIdentityStore.Save(_path, new DyAnchorIdentity("anchor-100", "room-200"));

            DyAnchorIdentity identity = DyTestIdentityStore.Load(_path);

            Assert.AreEqual("anchor-100", identity.AnchorId);
            Assert.AreEqual("room-200", identity.RoomId);
        }

        [Test]
        public void Load_AcceptsMarkdownListAndBackticks()
        {
            File.WriteAllText(
                _path,
                "# identity\n\n- AnchorId: `anchor-a`\n- RoomId: `room-b`\n");

            DyAnchorIdentity identity = DyTestIdentityStore.Load(_path);

            Assert.AreEqual("anchor-a", identity.AnchorId);
            Assert.AreEqual("room-b", identity.RoomId);
        }

        [Test]
        public void Load_RejectsMissingRoomId()
        {
            File.WriteAllText(_path, "AnchorId: `anchor-a`\nRoomId: ``\n");

            Assert.Throws<InvalidDataException>(() => DyTestIdentityStore.Load(_path));
        }

        [Test]
        public void BindAndBlind_KeepWireValueSix()
        {
            Assert.AreEqual(6, (int)RequestCode.Bind);
            Assert.AreEqual(6, (int)ActionCode.Bind);
#pragma warning disable CS0618
            Assert.AreEqual(RequestCode.Bind, RequestCode.Blind);
            Assert.AreEqual(ActionCode.Bind, ActionCode.Blind);
#pragma warning restore CS0618
        }
    }
}
