using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Supabase.Realtime;
using SupabaseExampleXA.Models;
using Xamarin.Forms;
using static Supabase.Client;

namespace SupabaseExampleXA
{
    public partial class MessageListPage : ContentPage
    {
        Models.Channel channel;
        Supabase.Realtime.Channel subscription;
        ObservableCollection<Message> messages { get; set; } = new ObservableCollection<Message>();

        public MessageListPage(Models.Channel channel)
        {
            InitializeComponent();

            this.channel = channel;
            Title = channel.Slug;

            MessageList.ItemsSource = messages;
            MessageEditor.Completed += MessageEditor_Completed;

            Init();
        }

        public async void Init()
        {
            var query = await Instance.From<Models.Message>().Filter("channel_id", Postgrest.Constants.Operator.Equals, channel.Id).Get();

            foreach (var model in query.Models)
                messages.Add(model);

            subscription = await Instance.From<Models.Message>().On(ChannelEventType.All, OnSubscriptionEvent);
        }

        private async void MessageEditor_Completed(object sender, EventArgs e)
        {
            await Instance.From<Models.Message>().Insert(new Message
            {
                Text = MessageEditor.Text,
                UserId = Instance.Auth.CurrentUser.Id,
                ChannelId = channel.Id
            });
            MessageEditor.Text = null;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (subscription != null)
                subscription.Unsubscribe();
        }



        private void OnSubscriptionEvent(object sender, SocketResponseEventArgs args)
        {
            switch (args.Response.Event)
            {
                case Constants.EventType.Insert:
                    var str = JsonConvert.SerializeObject(args.Response.Payload.Record);
                    var message = args.Response.Model<Message>();
                    messages.Add(message);
                    break;
            }
        }
    }
}
