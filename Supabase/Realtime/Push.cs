using System;
namespace Supabase.Realtime
{
    public class Push
    {
        private string channel;
        private string eventName;
        private object payload;
        private object response;
        private int timeout;
        private bool sent = false;

        public Push(string channel, string eventName, object payload, int timeout)
        {
            this.channel = channel;
            this.eventName = eventName;
            this.payload = payload;
            this.timeout = timeout;
        }

        private void resend(int timeout)
        {
            this.timeout = timeout;
            
        }


    }
}
