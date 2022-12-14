namespace Sentry.Maui.Tests.Mocks;

public class MockElement : Element
{
    public MockElement(string name = null)
    {
        // The x:Name attribute set in XAML is assigned to the StyleId property
        StyleId = name;
    }

    public event EventHandler CustomEvent;

    protected virtual void OnCustomEvent() => CustomEvent?.Invoke(this, EventArgs.Empty);

    public void RaiseCustomEvent() => OnCustomEvent();
}
