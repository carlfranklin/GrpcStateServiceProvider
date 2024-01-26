using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System.Text;
using Google.Protobuf;
using Microsoft.JSInterop;
using System.Runtime.CompilerServices;
using StateNotificationService;

namespace GrpcStateClient;
public class AppStateProviderBase : ComponentBase, IAsyncDisposable
{
    [Inject]
    public IJSRuntime _jsRuntime { get; set; }

    [Inject]
    public AppStateTransport.AppStateTransportClient _appStateTransportClient { get; set; }

    /// <summary>
    /// This will hold the property values
    /// </summary>
    public Dictionary<string, StateItem> Properties { get; set; } = new Dictionary<string, StateItem>();

    private string myId = string.Empty;

    protected object GetPropertyValue<T>([CallerMemberName] string propertyName = null)
    {
        if (Properties.TryGetValue(propertyName, out StateItem item))
        {
            // get the type from the item.TypeName property
            var type = Type.GetType(item.TypeName);
            var typeName = type.Name;
            object value1 = item.Value;
            if (value1 == null)
                return default(T);
            else
            {
                if (type.IsValueType)
                    value1 = (T)Convert.ChangeType(value1, typeof(T));
                else if (typeName == "String")
                    value1 = value1.ToString();
                else
                {
                    // Handle Nullable types
                    System.Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                    value1 = (T)Convert.ChangeType(value1, targetType);
                }
                return value1;
            }
        }
        else
        {
            // return the default value for this type
            return default(T);
        }
    }

    protected void SetPropertyValue(object value, [CallerMemberName] string propertyName = null)
    {
        // Add or update the property value in the dictionary
        // Create a StateItem from the value and propertyName
        var item = new StateItem
        {
            PropertyName = propertyName,
            TypeName = value.GetType().AssemblyQualifiedName,
            Value = value
        };

        Properties[propertyName] = item;

        // Sync to the server
        new Task(async () =>
        {
            await SyncState();
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
            await Init();
        }
    }

    public async Task Reload()
    {
        var request = new GetAppStateRequest();
        request.ClientId = myId;

        // go get the state
        AppStateMessage state = await _appStateTransportClient.GetAppStateAsync(request);

        // convert the state.Data to a byte array
        var data = state.Data.ToByteArray();

        if (data.Length > 0)
        {
            // convert bytes to json
            var json = Encoding.UTF8.GetString(data);

            // deserialize the json into the properties
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, StateItem>>(json);
            foreach (var kvp in dictionary)
            {
                var item = kvp.Value;
                // get the type from the item.TypeName property
                var type = System.Type.GetType(item.TypeName);
                object value = item.Value;

                JsonElement element = (JsonElement)value;

                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        item.Value = element.GetString();
                        break;
                    case JsonValueKind.Number:
                        item.Value = element.GetDecimal();
                        break;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        item.Value = element.GetBoolean();
                        break;
                    case JsonValueKind.Array:
                        item.Value = element.EnumerateArray().ToList();
                        break;
                    case JsonValueKind.Object:
                        item.Value = element.EnumerateObject().ToDictionary(k => k.Name, v => v.Value);
                        break;
                    default:
                        item.Value = null;
                        break;
                }
                if (!Properties.ContainsKey(kvp.Key))
                    Properties.Add(kvp.Key, item);
                else
                    Properties[kvp.Key].Value = item;
            }

        }
        StateHasChanged();
    }

    public async Task Init()
    {
        NotificationService.StateChanged += NotificationService_StateChanged;

        myId = await _jsRuntime.InvokeAsync<string>("getCookie", "stateBagId");
        if (string.IsNullOrEmpty(myId))
        {
            myId = Guid.NewGuid().ToString();
            await _jsRuntime.InvokeVoidAsync("setCookie", "stateBagId", myId, 365);
        }

        await Reload(); 
    }

    public async Task SyncState()
    {
        // serialize the Properties dictionary into Json
        var json = JsonSerializer.Serialize(Properties);

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
        await Reload();
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        NotificationService.StateChanged -= NotificationService_StateChanged;
        await SyncState();
    }
}
