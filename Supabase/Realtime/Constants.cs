using System;
namespace Supabase.Realtime
{
    public static class Constants
    {
        public static string VERSION = "1.0.0";

        public enum SocketStates
        {
            connecting = 0,
            open = 1,
            closing = 2,
            closed = 3
        }

        public enum EventType
        {
            Insert,
            Update,
            Delete
        }

        public static int DEFAULT_TIMEOUT = 10000;
        public static int WS_CLOSE_NORMAL = 1000;

        public static string CHANNEL_STATE_CLOSED = "closed";
        public static string CHANNEL_STATE_ERRORED = "errored";
        public static string CHANNEL_STATE_JOINED = "joined";
        public static string CHANNEL_STATE_JOINING = "joining";
        public static string CHANNEL_STATE_LEAVING = "leaving";

        public static string CHANNEL_EVENT_CLOSE = "phx_close";
        public static string CHANNEL_EVENT_ERROR = "phx_error";
        public static string CHANNEL_EVENT_JOIN = "phx_join";
        public static string CHANNEL_EVENT_REPLY = "phx_reply";
        public static string CHANNEL_EVENT_LEAVE = "phx_leave";

        public static string TANSPORT_WEBSOCKET = "websocket";
    }
}
