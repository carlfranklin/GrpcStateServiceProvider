using System;
using System.Reflection;

namespace StateNotificationService;

/// <summary>
/// This service is used to notify consumers of the state when it changes.
/// It exists in a separate library so that it can be used by both the GrpcStateClient and 
/// your application.
/// </summary>
public class NotificationService<T> where T : class
{
    // Singleton instance
    private static readonly NotificationService<T> instance = new NotificationService<T>();

    // Private member of type T
    private T state;

    // Private constructor to prevent instantiation from outside
    private NotificationService()
    {
        state = Activator.CreateInstance<T>();
    }

    // Public static method to get the instance of the class
    public static NotificationService<T> Instance
    {
        get => instance;
    }

    // Event that consumers can subscribe to for notifications of when the state changes
    public event EventHandler StateChanged;

    // Method that will raise the event to notify the other components
    public virtual void Notify()
    {
        StateChanged?.Invoke(this, new EventArgs());
    }

    // Method to set the value of a property by name
    public void SetProperty(string propertyName, object value)
    {
        PropertyInfo propertyInfo = typeof(T).GetProperty(propertyName);

        if (propertyInfo != null && propertyInfo.CanWrite)
        {
            object currentValue = propertyInfo.GetValue(state);
            if (!Equals(currentValue, value))
            {
                propertyInfo.SetValue(state, value);
            }
        }
        else
        {
            throw new ArgumentException("Property not found or not settable.", nameof(propertyName));
        }
    }

    // Create a method to get the value of a property by name
    public object GetProperty(string propertyName)
    {
        PropertyInfo propertyInfo = typeof(T).GetProperty(propertyName);

        if (propertyInfo != null && propertyInfo.CanRead)
        {
            return propertyInfo.GetValue(state);
        }
        else
        {
            throw new ArgumentException("Property not found or not readable.", nameof(propertyName));
        }
    }

    // Property to access the state
    public T State
    {
        get { return state; }
    }
}
