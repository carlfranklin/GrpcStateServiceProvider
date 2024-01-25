namespace GrpcStateClient;
public interface IAppState
{
    string Message { get; set; } 
    int Count { get; set; }
}
