using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SupabaseExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Be sure to set this in your Debug Options.
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");

            await Supabase.Client.InitializeAsync(url, key, new Supabase.SupabaseOptions { AutoConnectRealtime = true, ShouldInitializeRealtime = true });

            var reference = Supabase.Client.Instance.From<Models.Channel>();

            await reference.On(Supabase.Client.ChannelEventType.All, (sender, ev) =>
            {
                Debug.WriteLine($"[{ev.Response.Event}]:{ev.Response.Topic}:{ev.Response.Payload.Record}");
            });

            await reference.Insert(new Models.Channel { Slug = GenerateName(10), InsertedAt = DateTime.Now });

            Console.ReadLine();
        }

        // From: https://stackoverflow.com/a/49922533/3629438
        static string GenerateName(int len)
        {
            Random r = new Random();
            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
            string Name = "";
            Name += consonants[r.Next(consonants.Length)].ToUpper();
            Name += vowels[r.Next(vowels.Length)];
            int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < len)
            {
                Name += consonants[r.Next(consonants.Length)];
                b++;
                Name += vowels[r.Next(vowels.Length)];
                b++;
            }

            return Name;


        }
    }
}
