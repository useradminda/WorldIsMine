using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace WorldIsMine.Net.Protocol
{
    public sealed class MessageRouter
    {
        private readonly Dictionary<RouteKey, Action<NetPacket>> _handlers =
            new Dictionary<RouteKey, Action<NetPacket>>();

        public void Register<T>(
            RequestCode requestCode,
            ActionCode actionCode,
            MessageParser<T> parser,
            Action<T, NetPacket> handler)
            where T : IMessage<T>
        {
            if (parser == null)
                throw new ArgumentNullException(nameof(parser));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var key = new RouteKey(requestCode, actionCode);
            if (_handlers.ContainsKey(key))
                throw new InvalidOperationException($"Route already registered: {requestCode}/{actionCode}.");

            _handlers.Add(key, packet => handler(parser.ParseFrom(packet.Body), packet));
        }

        public bool Dispatch(NetPacket packet)
        {
            return _handlers.TryGetValue(
                       new RouteKey(packet.RequestCode, packet.ActionCode),
                       out Action<NetPacket> handler)
                   && Invoke(handler, packet);
        }

        private static bool Invoke(Action<NetPacket> handler, NetPacket packet)
        {
            handler(packet);
            return true;
        }

        private readonly struct RouteKey : IEquatable<RouteKey>
        {
            private readonly RequestCode _requestCode;
            private readonly ActionCode _actionCode;

            public RouteKey(RequestCode requestCode, ActionCode actionCode)
            {
                _requestCode = requestCode;
                _actionCode = actionCode;
            }

            public bool Equals(RouteKey other)
            {
                return _requestCode == other._requestCode && _actionCode == other._actionCode;
            }

            public override bool Equals(object obj)
            {
                return obj is RouteKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return ((int)_requestCode * 397) ^ (int)_actionCode;
            }
        }
    }
}
