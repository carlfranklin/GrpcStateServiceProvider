namespace GrpcStateClient;

public class StateItem
{
    public string PropertyName { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public object Value { get; set; } = null;
}
