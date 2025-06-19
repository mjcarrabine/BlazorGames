using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CardGamesClient;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<CardGamesLibrary.Shared.SolitaireGameService>();
builder.Services.AddScoped<CardGamesLibrary.Shared.WarGameService>();
builder.Services.AddScoped<CardGamesLibrary.Shared.BlackjackGameService>();
builder.Services.AddScoped<CardGamesLibrary.Shared.UnoGameService>();
builder.Services.AddScoped<CardGamesLibrary.Shared.GoFishService>();

await builder.Build().RunAsync();
