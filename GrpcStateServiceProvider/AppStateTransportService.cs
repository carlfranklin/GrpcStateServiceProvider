using Google.Protobuf;
using Grpc.Core;
using GrpcStateClient;
namespace GrpcStateServiceProvider;

public class AppStateTransportService : AppStateTransport.AppStateTransportBase
{
    public override Task<AppStateMessage> GetAppState(GetAppStateRequest request,
        ServerCallContext context)
    {
        var returnValue = new AppStateMessage();
        try
        {
            string clientId = request.ClientId;
            returnValue.ClientId = clientId;
            returnValue.ErrorMessage = "";

            if (!ServerSideStateBag.State.ContainsKey(clientId))
            {
                ServerSideStateBag.State.Add(clientId, new byte[0]);
            }

            returnValue.Data = ByteString.CopyFrom(ServerSideStateBag.State[clientId]);
            return Task.FromResult(returnValue);
        }
        catch (Exception ex)
        {
            returnValue.ErrorMessage = ex.Message;
            returnValue.Data = ByteString.Empty;
            return Task.FromResult(returnValue);
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
