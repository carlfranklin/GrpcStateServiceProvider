using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using Google.Protobuf;
using Microsoft.JSInterop;
using System.Runtime.CompilerServices;
using StateNotificationService;

namespace GrpcStateClient;

/// <summary>
/// This is a base component for an AppState component, which can be
/// created by the developer for their particular needs.
/// 
/// </summary>
public class AppStateProviderBase<T> : ComponentBase, IAsyncDisposable where T : class
{
    [Inject]
    public IJSRuntime _jsRuntime { get; set; }

    [Inject]
    public AppStateTransport.AppStateTransportClient _appStateTransportClient { get; set; }

    [Inject]
    public ILogger<AppStateProviderBase<T>> Logger { get; set; } = null!;

    [Inject]
    public NotificationService<T> NotificationService { get; set; }

    // Represents a uniuqe id for this client, saved as a cookie.
    private string myId = string.Empty;

    // Called by parent components to get a property value from AppState
    protected TProp GetPropertyValue<TProp>([CallerMemberName] string propertyName = null)
    {
        return (TProp)NotificationService.GetProperty(propertyName);
    }

    // Called by parent components to set a property value in AppState
    protected void SetPropertyValue(object value, [CallerMemberName] string propertyName = null)
    {
        NotificationService.SetProperty(propertyName, value);

        // Sync to the server (fire-and-forget)
        _ = UpdateStateOnServer();
        
        // Notify others that the state has changed
        NotificationService.Notify();

        // re-render the component
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // subscribe to the StateChanged event
            NotificationService.StateChanged += NotificationService_StateChanged;

            var jsLoader = new JavaScriptLoader(_jsRuntime);
            await jsLoader.LoadScriptAsync();

            try
            {
                // create a unique id for this client, or get it from a cookie
                myId = await _jsRuntime.InvokeAsync<string>("getCookie", "stateBagId");
                if (string.IsNullOrEmpty(myId))
                {
                    myId = Guid.NewGuid().ToString();
                    await _jsRuntime.InvokeVoidAsync("setCookie", "stateBagId", myId, 365);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read or create the stateBagId cookie; state will not be persisted to the server.");
            }
            Logger.LogDebug("State bag ID: {StateBagId}", myId);

            // load the state from the server
            await LoadStateFromServer();
        }
    }

    // Uses gRPC to get the the current AppState from the server
    public async Task LoadStateFromServer()
    {
        var request = new GetAppStateRequest();
        request.ClientId = myId;

        try
        {
            // go get the state
            AppStateMessage state = await _appStateTransportClient.GetAppStateAsync(request);

             // convert the state.Data to a byte array
            var data = state.Data.ToByteArray();

            // empty?
            if (data.Length == 0)
            {
                return;
            }

            // convert bytes to json
            var json = Encoding.UTF8.GetString(data);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            // deserialize the json into an AppState object
            var appState = JsonSerializer.Deserialize<T>(json);

            // set each property on the current AppState object using Reflection
            foreach (var property in appState.GetType().GetProperties())
            {
                var value = property.GetValue(appState);
                NotificationService.SetProperty(property.Name, value);
            }
            NotificationService.Notify();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load state from the server.");
        }
    }

    // Uses gRPC to update the AppState on the server
    public async Task UpdateStateOnServer()
    {
        try
        {
            // serialize the AppState object to json
            var json = JsonSerializer.Serialize(NotificationService.State);

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
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update state on the server.");
        }
    }

    // Handlle the NotificationService_StateChanged event
    private async void NotificationService_StateChanged(object sender, EventArgs e)
    {
        // force a re-render
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        // unsubscribe from the StateChanged event
        NotificationService.StateChanged -= NotificationService_StateChanged;
        // update the state on the server
        await UpdateStateOnServer();
    }
}
