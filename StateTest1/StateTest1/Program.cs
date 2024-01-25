using GrpcStateServiceProvider; // Required for the AppStateTransportService
using StateTest1.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Required for the AppStateTransportService
builder.Services.AddGrpc();

var app = builder.Build();

// Required for the AppStateTransportService
app.UseGrpcWeb();

// Required for the AppStateTransportService
app.MapGrpcService<AppStateTransportService>().EnableGrpcWeb();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StateTest1.Client._Imports).Assembly);

app.Run();
