using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PresenceExample;
using Supabase.Realtime;
using Supabase.Realtime.Socket;

var supabaseUrl = "https://ttbkuxsncbeeqnyeltrv.supabase.co";
var supabasePublicKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoiYW5vbiIsImlhdCI6MTYxMjIwMjE2MCwiZXhwIjoxOTI3Nzc4MTYwfQ.W03Slc1IFkth06FwutNVmorSKLLIjQ2f-bLJkNi51_Y";
var realtimeUrl = string.Format("{0}/realtime/v1", supabaseUrl).Replace("https", "wss");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddSingleton(builder => new Client(realtimeUrl, new ClientOptions
{
	Parameters = new SocketOptionsParameters
	{
		ApiKey = supabasePublicKey,
	}
}));

var constructed = builder.Build();

await constructed.RunAsync();
