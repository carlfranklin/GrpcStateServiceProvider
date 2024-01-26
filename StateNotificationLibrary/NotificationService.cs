namespace StateNotificationService;

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
