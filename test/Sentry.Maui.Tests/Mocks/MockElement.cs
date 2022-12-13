namespace Sentry.Maui.Tests.Mocks;

public class MockElement : Element
{
    public MockElement(string name = null)
    {
        StyleId = name;
    }

    public void InvokeOnChildAdded(Element child) =>
        OnChildAdded(child);

    public void InvokeOnChildRemoved(Element child, int oldLogicalIndex) =>
        OnChildRemoved(child, oldLogicalIndex);
}
