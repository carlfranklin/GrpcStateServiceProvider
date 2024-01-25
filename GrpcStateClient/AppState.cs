namespace GrpcStateClient;
public class AppState : IAppState
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}
