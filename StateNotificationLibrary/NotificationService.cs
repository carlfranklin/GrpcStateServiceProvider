namespace StateNotificationService;

/// <summary>
/// This service is used to notify consumers of the state when it changes.
/// It exists in a separate library so that it can be used by both the GrpcStateClient and 
/// your application.
/// </summary>
public static class NotificationService
{
    // create an event that consumers can subscribe to for notifications of when the state changes
    public static event EventHandler<StatePropertyChangedArgs> StateChanged;

    // create a method that will raise the event
    public static void OnStateChanged(StatePropertyChangedArgs args)
    {
        StateChanged?.Invoke(null, args);
    }
}
