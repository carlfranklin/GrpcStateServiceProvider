using Microsoft.JSInterop;
using System.Reflection;

namespace GrpcStateClient;

public class JavaScriptLoader
{
    private readonly IJSRuntime _jsRuntime;

    public JavaScriptLoader(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task LoadScriptAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "GrpcStateClient.grpcStateClient.js";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        var scriptContent = await reader.ReadToEndAsync();

        await _jsRuntime.InvokeVoidAsync("eval", scriptContent);
    }
}
