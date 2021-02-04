using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Supabase;
using Supabase.Gotrue;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Client = Supabase.Client;

namespace SupabaseExampleXA
{
    public partial class App : Application
    {
        private string supabaseCacheFilename = ".supabase.cache";

        public App()
        {
            InitializeComponent();

            MainPage = new LoadingPage();
        }

        protected override void OnStart()
        {
            InitSupabase();
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }

        async void InitSupabase()
        {
            var options = new SupabaseOptions
            {
                SessionPersistor = SessionPersistor,
                SessionRetriever = SessionRetriever,
                SessionDestroyer = SessionDestroyer
            };

            await Client.Initialize(Environment.GetEnvironmentVariable("supabaseUrl"), Environment.GetEnvironmentVariable("supabaseKey"), options);

            if (Client.Instance.Auth.CurrentSession == null)
            {
                await Client.Instance.Auth.SignIn(Environment.GetEnvironmentVariable("email"), Environment.GetEnvironmentVariable("password"));
            }

            await Client.Instance.Realtime.Connect();

            MainPage = new NavigationPage(new ChannelListPage());
        }

        internal Task<bool> SessionPersistor(Session session)
        {
            try
            {
                var cacheDir = FileSystem.CacheDirectory;
                var path = Path.Join(cacheDir, supabaseCacheFilename);
                var str = JsonConvert.SerializeObject(session);

                using (StreamWriter file = new StreamWriter(path))
                {
                    file.Write(str);
                    file.Dispose();
                    return Task.FromResult(true);

                };
            }
            catch (Exception err)
            {
                Debug.WriteLine("Unable to write cache file.");
                throw err;
            }
        }

        internal Task<Session> SessionRetriever()
        {
            var tsc = new TaskCompletionSource<Session>();
            try
            {
                var cacheDir = FileSystem.CacheDirectory;
                var path = Path.Join(cacheDir, supabaseCacheFilename);

                if (File.Exists(path))
                {
                    using (StreamReader file = new StreamReader(path))
                    {
                        var str = file.ReadToEnd();
                        if (!String.IsNullOrEmpty(str))
                            tsc.SetResult(JsonConvert.DeserializeObject<Session>(str));
                        else
                            tsc.SetResult(null);
                        file.Dispose();
                    };
                }
                else
                {
                    tsc.SetResult(null);
                }
            }
            catch
            {
                Debug.WriteLine("Unable to read cache file.");
                tsc.SetResult(null);
            }
            return tsc.Task;

        }

        internal Task<bool> SessionDestroyer()
        {
            try
            {
                var cacheDir = FileSystem.CacheDirectory;
                var path = Path.Join(cacheDir, supabaseCacheFilename);
                if (File.Exists(path))
                    File.Delete(path);
                return Task.FromResult(true);
            }
            catch (Exception err)
            {
                Debug.WriteLine("Unable to delete cache file.");
                return Task.FromResult(false);
            }
        }
    }
}
