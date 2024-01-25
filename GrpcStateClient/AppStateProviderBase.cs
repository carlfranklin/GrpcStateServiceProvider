using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System.Text;
using Google.Protobuf;
using Microsoft.JSInterop;
using System.Runtime.CompilerServices;

namespace GrpcStateClient;
public class AppStateProviderBase : ComponentBase, IAsyncDisposable
{
    [Inject]
    public IJSRuntime _jsRuntime { get; set; }

    [Inject]
    public AppStateTransport.AppStateTransportClient _appState { get; set; }

    private List<EventCallback<StatePropertyChangedArgs>> Callbacks
       = new List<EventCallback<StatePropertyChangedArgs>>();

    // Each component will register a callback
    public void RegisterCallback(EventCallback<StatePropertyChangedArgs> callback)
    {
        // Only add if we have not already registered this callback
        if (!Callbacks.Contains(callback))
        {
            Callbacks.Add(callback);
        }
    }

    // We call this from our property setters
    private void NotifyPropertyChanged(StatePropertyChangedArgs args)
    {
        foreach (var callback in Callbacks)
        {
            // Ignore exceptions due to dangling references
            try
            {
                // Invoke the callback
                callback.InvokeAsync(args);
            }
            catch { }
        }
    }

    private string myId = string.Empty;

    public IAppState State { get; set; }

    protected void PropertyChanged(object value, [CallerMemberName] string propertyName = null)
    {
        new Task(async () =>
        {
            await SyncState();
        }).Start();
        NotifyPropertyChanged(new(propertyName, value));
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Init();
        }
    }

    public async Task Init()
    {
        myId = await _jsRuntime.InvokeAsync<string>("getCookie", "stateBagId");
        if (string.IsNullOrEmpty(myId))
        {
            myId = Guid.NewGuid().ToString();
            await _jsRuntime.InvokeVoidAsync("setCookie", "stateBagId", myId, 365);
        }

        var request = new GetAppStateRequest();
        request.ClientId = myId;

        // go get the state
        AppStateMessage state = await _appState.GetAppStateAsync(request);

        var data = state.Data.ToByteArray();

        if (data.Length > 0)
        {
            // convert data to json
            var json = Encoding.UTF8.GetString(data);

            // deserialize the json into the properties
            var appState = JsonSerializer.Deserialize<AppState>(json);

            // set the properties
            var t = typeof(IAppState);
            var props = t.GetProperties();
            foreach (var prop in props)
            {
                object value = prop.GetValue(appState);
                prop.SetValue(State, value, null);
            }
        }
        StateHasChanged();
        // otherwise this is the first time.
    }

    public async Task SyncState()
    {
        // serialize the state properties into json
        var json = JsonSerializer.Serialize(State);

        // convert to a byte array
        var bytes = Encoding.UTF8.GetBytes(json);

        // update the state on the server
        var state = new AppStateMessage
        {
            ClientId = myId,
            ErrorMessage = "",
            Data = ByteString.CopyFrom(bytes)
        };
        await _appState.UpdateAppStateAsync(state);
    }

    public async ValueTask DisposeAsync()
    {
        await SyncState();
    }
}
