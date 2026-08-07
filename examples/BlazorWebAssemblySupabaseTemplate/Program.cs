using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWebAssemblySupabaseTemplate;
using Microsoft.AspNetCore.Components.Authorization;
using BlazorWebAssemblySupabaseTemplate.Providers;
using BlazorWebAssemblySupabaseTemplate.Services;
using Blazored.LocalStorage;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();


// ---------- BLAZOR AUTH
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>(
	provider => new CustomAuthStateProvider(
		provider.GetRequiredService<ILocalStorageService>(),
		provider.GetRequiredService<Supabase.Client>(),
		provider.GetRequiredService<ILogger<CustomAuthStateProvider>>()
	)
)
	;
builder.Services.AddAuthorizationCore();

// ---------- SUPABASE
var url = "https://pylnesfgmytjegzzculn.supabase.co";
var key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InB5bG5lc2ZnbXl0amVnenpjdWxuIiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjgyOTMwMzcsImV4cCI6MTk4Mzg2OTAzN30.kI29Q_qYWDH5SD6oi5NTwHG6Pxy1e1AUfR8s_ga45lE";

builder.Services.AddScoped<Supabase.Client>(
	provider => new Supabase.Client(
		url,
		key,
		new Supabase.SupabaseOptions
		{
			AutoRefreshToken = true,
			AutoConnectRealtime = true,
			SessionHandler = new CustomSupabaseSessionHandler(
				provider.GetRequiredService<ILocalStorageService>(),
				provider.GetRequiredService<ILogger<CustomSupabaseSessionHandler>>()
			)
		}
	)
);

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<StorageService>();

await builder.Build().RunAsync();