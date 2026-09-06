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
    public void Instances_AreIndependent()
    {
        var first = new NotificationService<CounterState>();
        var second = new NotificationService<CounterState>();
        first.SetProperty("Count", 5);
        Assert.Equal(0, second.GetProperty("Count"));
    }

    [Fact]
    public void GetProperty_ReturnsDefault_ForNewState()
    {
        Assert.Equal(0, new NotificationService<CounterState>().GetProperty("Count"));
    }

    [Fact]
    public void SetProperty_UpdatesValue()
    {
        var service = new NotificationService<TextState>();
        service.SetProperty("Name", "Buzzby");
        Assert.Equal("Buzzby", service.GetProperty("Name"));
    }

    [Fact]
    public void SetProperty_ThrowsArgumentException_ForUnknownProperty()
    {
        Assert.Throws<ArgumentException>(
            () => new NotificationService<CounterState>().SetProperty("Missing", 1));
    }

    [Fact]
    public void GetProperty_ThrowsArgumentException_ForUnknownProperty()
    {
        Assert.Throws<ArgumentException>(
            () => new NotificationService<CounterState>().GetProperty("Missing"));
    }

    [Fact]
    public void SetProperty_ThrowsArgumentException_ForReadOnlyProperty()
    {
        Assert.Throws<ArgumentException>(
            () => new NotificationService<ReadWriteState>().SetProperty("ReadOnlyValue", 1));
    }

    [Fact]
    public void Notify_RaisesStateChanged()
    {
        var service = new NotificationService<CounterState>();
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
