using System.Text;
using System.Text.Json;
using GrpcStateClient;
using GrpcStateServiceProvider;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrpcStateTests;

public class AppStateTransportServiceTests
{
    private readonly AppStateTransportService _service;

    public AppStateTransportServiceTests()
    {
        ServerSideStateBag.State.Clear();
        _service = new AppStateTransportService(NullLogger<AppStateTransportService>.Instance);
    }

    [Fact]
    public async Task GetAppState_UnknownClient_ReturnsEmptyDataAndCreatesEntry()
    {
        var response = await _service.GetAppState(new GetAppStateRequest { ClientId = "client-1" }, null!);

        Assert.Equal("client-1", response.ClientId);
        Assert.Equal("", response.ErrorMessage);
        Assert.Empty(response.Data.ToByteArray());
        Assert.True(ServerSideStateBag.State.ContainsKey("client-1"));
    }

    [Fact]
    public async Task UpdateAppState_StoresData()
    {
        var response = await _service.UpdateAppState(new AppStateMessage
        {
            ClientId = "client-2",
            Data = Google.Protobuf.ByteString.CopyFromUtf8("hello")
        }, null!);

        Assert.Equal("", response.Message);
        Assert.Equal("hello", Encoding.UTF8.GetString(ServerSideStateBag.State["client-2"]));
    }

    [Fact]
    public async Task GetAppState_ReturnsPreviouslyStoredData()
    {
        await _service.UpdateAppState(new AppStateMessage
        {
            ClientId = "client-3",
            Data = Google.Protobuf.ByteString.CopyFromUtf8("{\"count\":7}")
        }, null!);

        var response = await _service.GetAppState(new GetAppStateRequest { ClientId = "client-3" }, null!);

        Assert.Equal("{\"count\":7}", Encoding.UTF8.GetString(response.Data.ToByteArray()));
    }

    [Fact]
    public async Task StateJson_RoundTripsThroughServer()
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(new CounterState { Count = 7 });

        await _service.UpdateAppState(new AppStateMessage
        {
            ClientId = "client-4",
            Data = Google.Protobuf.ByteString.CopyFrom(json)
        }, null!);

        var response = await _service.GetAppState(new GetAppStateRequest { ClientId = "client-4" }, null!);
        var restored = JsonSerializer.Deserialize<CounterState>(response.Data.ToByteArray());

        Assert.Equal(7, restored!.Count);
    }

    private class CounterState
    {
        public int Count { get; set; }
    }
}
