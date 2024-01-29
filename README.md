# Adding State to Blazor 8 per/page component apps

Step 1 is to create a new Blazor Web App using .NET 8.

Add the following package to the the solution (both projects):

```
GrpcStateServiceProvider
```

### Server Configuration

To the server project, add the following packages:

```xml
<PackageReference Include="Grpc.AspNetCore" Version="2.60.0" />
<PackageReference Include="Grpc.AspNetCore.Web" Version="2.60.0" />
```

Add the following to the *Program.cs* file:

```c#
using GrpcStateServiceProvider; // Required for the AppStateTransportService
```

```c#
// Required for the AppStateTransportService
builder.Services.AddGrpc();
```

```c#
// Required for the AppStateTransportService
app.UseGrpcWeb();
// Required for the AppStateTransportService
app.MapGrpcService<AppStateTransportService>().EnableGrpcWeb();
```

### Client Configuration

To the client project, add the following packages:

```xml
<PackageReference Include="Google.Protobuf" Version="3.25.2" />
<PackageReference Include="Grpc.Net.Client" Version="2.60.0" />
<PackageReference Include="Grpc.Net.Client.Web" Version="2.60.0" />
<PackageReference Include="Grpc.Tools" Version="2.60.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

To the client project's *Program.cs* file, add the following:

```c#
// required for accessing AppStateTransportService 
using Grpc.Net.Client.Web;
using Grpc.Net.Client;
using GrpcStateClient;
using Microsoft.AspNetCore.Components;
```

```c#
// Required for calling the AppStateTransportService via GrpcStateClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = 
    new Uri(builder.HostEnvironment.BaseAddress) });

// Required for calling the AppStateTransportService via GrpcStateClient
builder.Services.AddSingleton(services =>
{
    var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
    var baseUri = services.GetRequiredService<NavigationManager>().BaseUri;
    var channel = GrpcChannel.ForAddress(baseUri, new GrpcChannelOptions { HttpClient = httpClient });
    return new AppStateTransport.AppStateTransportClient(channel);
});
```

To the *_Imports.razor*, add the following:

```
@using GrpcStateClient
@using StateNotificationService
```

To the client project, add the following two .cs files:

*IAppState.cs*

```c#
public interface IAppState
{
    string Message { get; set; }
    int Count { get; set; }
}
```

*AppState.cs*:

```c#
public class AppState : IAppState
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}
```

You can, of course, modify these two classes to fit your needs.

To the client project, add a new Razor component: 

*AppStateProvider.razor*

```C#
@inherits AppStateProviderBase<AppState>
@implements IAppState

@ChildContent

@code {

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    public string Message
    {
        get => (string)GetPropertyValue<string>();
        set => SetPropertyValue(value);
    }

    public int Count
    {
        get => (int)GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }
}
```

### Usage

Replace *Counter.razor* with the following:

```c#
@page "/counter"
@rendermode InteractiveWebAssembly

<PageTitle>Counter</PageTitle>

@if (!loaded)
{
    <p><em>Loading...</em></p>
}
else
{
    <AppStateProvider @ref=AppState>

        <h1>Counter</h1>

        <p role="status">Current count: @AppState.Count</p>

        <button class="btn btn-primary" @onclick="IncrementCount">Increment Counter</button>

        <button class="btn btn-primary" @onclick="UpdateMessage">Update Message</button>

    </AppStateProvider>
}

@code {

    bool loaded = false;

    private AppStateProvider AppState { get; set; }

    private void IncrementCount()
    {
        AppState.Count++;
    }

    private void UpdateMessage()
    {
        AppState.Message = $"Hello from Counter at {DateTime.Now.ToLongTimeString()}";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            loaded = true;
            StateHasChanged();
        }
    }
}
```

Run the app, and go to the Counter page. Increment the counter, go to the Home page, and back to the Counter page. The current count will be maintained.

### Notification Demo

This exercise will show off the notification feature, where each AppStateProvider will be notified when an AppState property changes.

To the client project, add the following Razor component:

*Toolbar.razor*:

```c#
@rendermode InteractiveWebAssembly

@if (loaded)
{
    <AppStateProvider @ref=AppState>
        @AppState.Message
    </AppStateProvider>
}

@code {

    bool loaded = false;
    private AppStateProvider AppState { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            loaded = true;
            StateHasChanged();
        }
    }
}
```

Modify *MainLayout.razor* to show the Toolbar:

```html
@inherits LayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>

    <main>
        <div class="top-row px-4">
            <Toolbar />
        </div>

        <article class="content px-4">
            @Body
        </article>
    </main>
</div>

<div id="blazor-error-ui">
    An unhandled error has occurred.
    <a href="" class="reload">Reload</a>
    <a class="dismiss">🗙</a>
</div>
```

Run the app, navigate to the Counter page, and click the **Update Message** button. The message will show in the Counter.

