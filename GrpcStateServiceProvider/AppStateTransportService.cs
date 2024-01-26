using Google.Protobuf;
using Grpc.Core;
using GrpcStateClient;
namespace GrpcStateServiceProvider;

public class AppStateTransportService : AppStateTransport.AppStateTransportBase
{
    public override Task<AppStateMessage> GetAppState(GetAppStateRequest request,
        ServerCallContext context)
    {
        var response = new AppStateMessage();
        try
        {
            string clientId = request.ClientId;
            response.ClientId = clientId;
            response.ErrorMessage = "";

            if (!ServerSideStateBag.State.ContainsKey(clientId))
            {
                ServerSideStateBag.State.Add(clientId, new byte[0]);
            }

            response.Data = ByteString.CopyFrom(ServerSideStateBag.State[clientId]);
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
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
            if (!ServerSideStateBag.State.ContainsKey(clientId))
            {
                ServerSideStateBag.State.Add(clientId, data);
            }
            else
            {
                ServerSideStateBag.State[clientId] = data;
            }

            return Task.FromResult(new UpdateAppStateResponse
            {
                Message = ""
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new UpdateAppStateResponse
            {
                Message = ex.Message
            });
        }
    }
}
