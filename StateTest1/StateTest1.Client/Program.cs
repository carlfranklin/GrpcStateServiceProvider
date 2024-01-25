using Grpc.Net.Client.Web;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GrpcStateClient;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Required for calling the AppStateTransportService
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = 
    new Uri(builder.HostEnvironment.BaseAddress) });

// Required for calling the AppStateTransportService
builder.Services.AddSingleton(services =>
{
    var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
    var baseUri = services.GetRequiredService<NavigationManager>().BaseUri;
    var channel = GrpcChannel.ForAddress(baseUri, new GrpcChannelOptions { HttpClient = httpClient });
    return new AppStateTransport.AppStateTransportClient(channel);
});

await builder.Build().RunAsync();
