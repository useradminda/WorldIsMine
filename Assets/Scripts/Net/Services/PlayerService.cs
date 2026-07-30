using System;
using System.Threading;
using System.Threading.Tasks;
using ClientProtocol;
using Google.Protobuf;
using WorldIsMine.Net.Protocol;
using WorldIsMine.Net.Runtime;
using WorldIsMine.Net.Transport;

namespace WorldIsMine.Net.Services
{
    public sealed class PlayerService
    {
        private readonly MainThreadDispatcher _mainThread;

        public PlayerService(
            MessageRouter router,
            MainThreadDispatcher mainThread)
        {
            if (router == null)
                throw new ArgumentNullException(nameof(router));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));

            router.Register(
                RequestCode.S2CPlayerEnter,
                ActionCode.None,
                LivePlayerEnterNotify.Parser,
                OnPlayerEnter);
            router.Register(
                RequestCode.S2CPlayerLeave,
                ActionCode.None,
                LivePlayerLeaveNotify.Parser,
                OnPlayerLeave);
            router.Register(
                RequestCode.S2CPlayerCampSelected,
                ActionCode.None,
                LivePlayerCampSelectedNotify.Parser,
                OnPlayerCampSelected);
            router.Register(
                RequestCode.S2CPlayerGift,
                ActionCode.None,
                LivePlayerGiftNotify.Parser,
                OnPlayerGift);
        }

        public event Action<LivePlayerEnterNotify> PlayerEntered;
        public event Action<LivePlayerLeaveNotify> PlayerLeft;
        public event Action<LivePlayerCampSelectedNotify> PlayerCampSelected;
        public event Action<LivePlayerGiftNotify> PlayerGifted;

        private void OnPlayerEnter(LivePlayerEnterNotify notify, NetPacket packet)
        {
            _mainThread.Post(() => PlayerEntered?.Invoke(notify));
        }

        private void OnPlayerLeave(LivePlayerLeaveNotify notify, NetPacket packet)
        {
            _mainThread.Post(() => PlayerLeft?.Invoke(notify));
        }

        private void OnPlayerCampSelected(
            LivePlayerCampSelectedNotify notify,
            NetPacket packet)
        {
            _mainThread.Post(() => PlayerCampSelected?.Invoke(notify));
        }

        private void OnPlayerGift(LivePlayerGiftNotify notify, NetPacket packet)
        {
            _mainThread.Post(() => PlayerGifted?.Invoke(notify));
        }
    }

    public sealed class LiveTestService
    {
        private readonly TcpTransport _transport;
        private readonly MainThreadDispatcher _mainThread;

        public LiveTestService(
            TcpTransport transport,
            MessageRouter router,
            MainThreadDispatcher mainThread)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
            if (router == null)
                throw new ArgumentNullException(nameof(router));

            router.Register(
                RequestCode.S2CLiveClientTest,
                ActionCode.None,
                LiveClientTestResponse.Parser,
                OnResponse);
        }

        public event Action<LiveClientTestResponse> ResponseReceived;

        public Task<long> SendAsync(
            LiveClientTestRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _transport.SendAsync(
                RequestCode.C2SLiveClientTest,
                ActionCode.None,
                request.ToByteArray(),
                cancellationToken);
        }

        private void OnResponse(LiveClientTestResponse response, NetPacket packet)
        {
            _mainThread.Post(() => ResponseReceived?.Invoke(response));
        }
    }
}
