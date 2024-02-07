using Grpc.Net.Client.Web;
using Grpc.Net.Client;
using GrpcStateClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;

namespace GrpcStateServiceProvider;

public static class StateServerServiceCollectionExtensions
{
    public static IServiceCollection ConfigureStateServices(this IServiceCollection services, Uri serverBaseUri)
    {
        services.AddGrpc();

        services.AddScoped(sp => new HttpClient
        {
            BaseAddress = serverBaseUri
        });

        // Required for calling the AppStateTransportService via GrpcStateClient
        services.AddSingleton(services =>
        {
            var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
            var baseUri = serverBaseUri;
            var channel = GrpcChannel.ForAddress(baseUri, new GrpcChannelOptions { HttpClient = httpClient });
            return new AppStateTransport.AppStateTransportClient(channel);
        });

        return services;
    }

    public static WebApplication ConfigureStateServerMiddleware(this WebApplication app)
    {
        app.UseGrpcWeb();
        app.MapGrpcService<AppStateTransportService>().EnableGrpcWeb();

        return app;
    }
}