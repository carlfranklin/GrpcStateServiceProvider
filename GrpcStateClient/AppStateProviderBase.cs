using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System.Text;
using Google.Protobuf;
using Microsoft.JSInterop;
using System.Runtime.CompilerServices;
using StateNotificationService;
using StateTypes;

namespace GrpcStateClient;

/// <summary>
/// This is a base component for an AppState component, which can be
/// created by the developer for their particular needs.
/// 
/// </summary>
public class AppStateProviderBase : ComponentBase, IAsyncDisposable
{
    [Inject]
    public IJSRuntime _jsRuntime { get; set; }

    [Inject]
    public AppStateTransport.AppStateTransportClient _appStateTransportClient { get; set; }

    // required for JavaScript interop
    private Lazy<Task<IJSObjectReference>> moduleTask;

    // AppState is defined in StateTypes, outside of this project so it can
    // be easily modified by the developer
    private AppState AppState { get; set; } = null;
    
    // Represents a uniuqe id for this client, saved as a cookie.
    private string myId = string.Empty;

    // Ensures that AppState is not null
    private void EnsureAppState()
    {
        if (AppState == null)
        {
            // go get the appstate from the server if it's null
            new Task(async () =>
            {
                await LoadStateFromServer();
            }).Start();

            // If the server doesn't have it, return a new appstate
            if (AppState == null)
            {
                AppState = new AppState();
            }
        }
    }

    // Called by parent components to get a property value from AppState
    protected T GetPropertyValue<T>([CallerMemberName] string propertyName = null)
    {
        EnsureAppState();
        // use Reflection to get the property value from AppState
        var property = AppState.GetType().GetProperty(propertyName);
        return (T)property.GetValue(AppState);
    }

    // Called by parent components to set a property value in AppState
    protected void SetPropertyValue(object value, [CallerMemberName] string propertyName = null)
    {
        EnsureAppState();

        // use Reflection to set the property value on AppState
        var property = AppState.GetType().GetProperty(propertyName);
        property.SetValue(AppState, value);

        // Sync to the server
        new Task(async () =>
        {
            await UpdateStateOnServer();
        }).Start();

        // Notify any components that the property has changed
        NotificationService.OnStateChanged(new StatePropertyChangedArgs(propertyName, value));

        // re-render the component
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // subscribe to the StateChanged event
            NotificationService.StateChanged += NotificationService_StateChanged;

            moduleTask = new(() => _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/GrpcStateClient/JsInterop.js").AsTask());

            var module = await moduleTask.Value;

            // create a unique id for this client, or get it from a cookie
            myId = await module.InvokeAsync<string>("getCookie", "stateBagId");
            if (string.IsNullOrEmpty(myId))
            {
                myId = Guid.NewGuid().ToString();
                await module.InvokeVoidAsync("setCookie", "stateBagId", myId, 365);
            }
            // load the state from the server
            await LoadStateFromServer();
        }
    }

    // Uses gRPC to get the the current AppState from the server
    public async Task LoadStateFromServer()
    {
        var request = new GetAppStateRequest();
        request.ClientId = myId;

        // go get the state
        AppStateMessage state = await _appStateTransportClient.GetAppStateAsync(request);

        // null?
        if (state == null)
        {
            // create a new AppState if it's still null
            if (AppState == null)
                AppState = new AppState();
            return;
        }

        // convert the state.Data to a byte array
        var data = state.Data.ToByteArray();

        // empty?
        if (data.Length == 0)
        {
            // create a new AppState if it's still null
            if (AppState == null)
                AppState = new AppState();
            return;
        }

        // convert bytes to json
        var json = Encoding.UTF8.GetString(data);

        // deserialize the json into an AppState object
        var appState = JsonSerializer.Deserialize<AppState>(json);

        // set each property on the current AppState object using Reflection
        foreach (var property in appState.GetType().GetProperties())
        {
            var value = property.GetValue(appState);
            property.SetValue(AppState, value);
        }
        StateHasChanged();
    }

    // Uses gRPC to update the AppState on the server
    public async Task UpdateStateOnServer()
    {
        // serialize the AppState object to json
        var json = JsonSerializer.Serialize(AppState);

        // convert to a byte array
        var bytes = Encoding.UTF8.GetBytes(json);

        // update the state on the server
        var state = new AppStateMessage
        {
            ClientId = myId,
            ErrorMessage = "",
            Data = ByteString.CopyFrom(bytes)
        };
        await _appStateTransportClient.UpdateAppStateAsync(state);
    }

    // Handlle the NotificationService_StateChanged event
    private async void NotificationService_StateChanged(object sender, StatePropertyChangedArgs e)
    {
        // update the property value on AppState
        var property = AppState.GetType().GetProperty(e.PropertyName);
        property.SetValue(AppState, e.NewValue);
        // force a re-render
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
        // unsubscribe from the StateChanged event
        NotificationService.StateChanged -= NotificationService_StateChanged;
        // update the state on the server
        await UpdateStateOnServer();
    }
}
