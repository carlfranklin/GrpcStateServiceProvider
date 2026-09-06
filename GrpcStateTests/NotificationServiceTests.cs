using StateNotificationService;

namespace GrpcStateTests;

public class NotificationServiceTests
{
    private class CounterState
    {
        public int Count { get; set; }
    }

    private class TextState
    {
        public string? Name { get; set; }
    }

    private class ReadWriteState
    {
        public int Value { get; set; }
        public int ReadOnlyValue => 42;
    }

    [Fact]
    public void Instance_IsSingleton()
    {
        Assert.Same(NotificationService<CounterState>.Instance, NotificationService<CounterState>.Instance);
    }

    [Fact]
    public void GetProperty_ReturnsDefault_ForNewState()
    {
        Assert.Equal(0, NotificationService<CounterState>.Instance.GetProperty("Count"));
    }

    [Fact]
    public void SetProperty_UpdatesValue()
    {
        var service = NotificationService<TextState>.Instance;
        service.SetProperty("Name", "Buzzby");
        Assert.Equal("Buzzby", service.GetProperty("Name"));
    }

    [Fact]
    public void SetProperty_ThrowsArgumentException_ForUnknownProperty()
    {
        Assert.Throws<ArgumentException>(
            () => NotificationService<CounterState>.Instance.SetProperty("Missing", 1));
    }

    [Fact]
    public void GetProperty_ThrowsArgumentException_ForUnknownProperty()
    {
        Assert.Throws<ArgumentException>(
            () => NotificationService<CounterState>.Instance.GetProperty("Missing"));
    }

    [Fact]
    public void SetProperty_ThrowsArgumentException_ForReadOnlyProperty()
    {
        Assert.Throws<ArgumentException>(
            () => NotificationService<ReadWriteState>.Instance.SetProperty("ReadOnlyValue", 1));
    }

    [Fact]
    public void Notify_RaisesStateChanged()
    {
        var service = NotificationService<CounterState>.Instance;
        bool raised = false;
        EventHandler handler = (s, e) => raised = true;
        service.StateChanged += handler;
        try
        {
            service.Notify();
        }
        finally
        {
            service.StateChanged -= handler;
        }
        Assert.True(raised);
    }
}
