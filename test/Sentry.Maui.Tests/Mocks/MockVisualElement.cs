namespace Sentry.Maui.Tests.Mocks;

public class MockVisualElement : VisualElement
{
    public MockVisualElement(string name = null)
    {
        // The x:Name attribute set in XAML is assigned to the StyleId property
        StyleId = name;
    }
}
