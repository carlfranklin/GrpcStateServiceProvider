using Google.Protobuf;
using Grpc.Core;
using GrpcStateClient;
using Microsoft.Extensions.Logging;

namespace GrpcStateServiceProvider;

public class AppStateTransportService : AppStateTransport.AppStateTransportBase
{
    private readonly ILogger<AppStateTransportService> _logger;

    public AppStateTransportService(ILogger<AppStateTransportService> logger)
    {
        _logger = logger;
    }

    public override Task<AppStateMessage> GetAppState(GetAppStateRequest request,
        ServerCallContext context)
    {
        var response = new AppStateMessage();
        try
        {
            string clientId = request.ClientId;
            response.ClientId = clientId;
            response.ErrorMessage = "";

            ServerSideStateBag.State.TryAdd(clientId, new byte[0]);

            response.Data = ByteString.CopyFrom(ServerSideStateBag.State[clientId]);
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAppState failed for client {ClientId}.", request.ClientId);
            response.ErrorMessage = ex.Message;
            response.Data = ByteString.Empty;
            return Task.FromResult(response);
        }
    }

    public override Task<UpdateAppStateResponse> UpdateAppState(AppStateMessage request,
        ServerCallContext context)
    {
        string clientId = request.ClientId;
        var data = request.Data.ToByteArray();

        try
        {
            ServerSideStateBag.State[clientId] = data;

            return Task.FromResult(new UpdateAppStateResponse
            {
                Message = ""
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateAppState failed for client {ClientId}.", clientId);
            return Task.FromResult(new UpdateAppStateResponse
            {
                Message = ex.Message
            });
        }
    }
}
