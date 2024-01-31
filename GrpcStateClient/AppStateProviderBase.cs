using Microsoft.AspNetCore.Components;
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
    public HttpClient httpClient { get; set; }

    private NotificationService<T> notificationService;

    // Represents a uniuqe id for this client, saved as a cookie.
    private string myId = string.Empty;

    // Called by parent components to get a property value from AppState
    protected T GetPropertyValue<T>([CallerMemberName] string propertyName = null)
    {
        return (T)notificationService.GetProperty(propertyName);
    }

    // Called by parent components to set a property value in AppState
    protected void SetPropertyValue(object value, [CallerMemberName] string propertyName = null)
    {
        notificationService.SetProperty(propertyName, value);

        // Sync to the server
        new Task(async () =>
        {
            await UpdateStateOnServer();
        }).Start();
        
        // Notify others that the state has changed
        notificationService.Notify();

        // re-render the component
        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        notificationService = NotificationService<T>.Instance;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // subscribe to the StateChanged event
            notificationService.StateChanged += NotificationService_StateChanged;

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

            }
            await _jsRuntime.InvokeVoidAsync("console.log", $"myID={myId}");

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
                notificationService.SetProperty(property.Name, value);
            }
            notificationService.Notify();
            StateHasChanged();
        }
        catch (Exception ex)
        {

        }
    }

    // Uses gRPC to update the AppState on the server
    public async Task UpdateStateOnServer()
    {
        // serialize the AppState object to json
        var json = JsonSerializer.Serialize(notificationService.State);

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
    private async void NotificationService_StateChanged(object sender, EventArgs e)
    {
        // force a re-render
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        // unsubscribe from the StateChanged event
        notificationService.StateChanged -= NotificationService_StateChanged;
        // update the state on the server
        await UpdateStateOnServer();
    }
}
