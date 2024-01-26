namespace StateTypes;

public class AppState :IAppState
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}
