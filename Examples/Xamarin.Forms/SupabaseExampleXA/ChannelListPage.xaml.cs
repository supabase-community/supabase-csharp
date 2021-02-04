using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupabaseExampleXA.Models;
using Xamarin.Forms;

namespace SupabaseExampleXA
{
    public partial class ChannelListPage : ContentPage
    {
        public ChannelListPage()
        {
            InitializeComponent();

            ChannelList.ItemSelected += ChannelList_ItemSelected;
        }

        private void ChannelList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (ChannelList.SelectedItem == null) return;

            Navigation.PushAsync(new MessageListPage(ChannelList.SelectedItem as Channel));
            ChannelList.SelectedItem = null;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Device.BeginInvokeOnMainThread(async () => await Refresh());

            Supabase.Client.Instance.Auth.StateChanged += async (object sender, Supabase.Gotrue.ClientStateChanged e) =>
            {
                if (e.State == Supabase.Gotrue.Client.AuthState.SignedIn)
                {
                    await Refresh();
                }
            };
        }

        private async Task Refresh()
        {
            var channels = await Supabase.Client.Instance.From<Channel>().Get();
            ChannelList.ItemsSource = channels?.Models;
        }
    }
}
