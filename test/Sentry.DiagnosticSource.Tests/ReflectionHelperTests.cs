using Sentry.Internal.DiagnosticSource;

namespace Sentry.DiagnosticSource.Tests;

public class ReflectionHelperTests
{
    public class MyClass
    {
        public string SomeString { get; set; }
        public MySubclass Subclass { get; set; }
    }

    public class MySubclass
    {
        public string AnotherString { get; set; }
    }

    [Fact]
    public void GetStringProperty_ShouldReturnTopLevelProperty()
    {
        var myClass = new MyClass
        {
            SomeString = "Hello"
        };

        var result = myClass.GetStringProperty("SomeString");

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void GetStringProperty_ShouldReturnNestedProperty()
    {
        var myClass = new MyClass
        {
            Subclass = new MySubclass
            {
                AnotherString = "World"
            }
        };

        var result = myClass.GetStringProperty("Subclass.AnotherString");

        Assert.Equal("World", result);
    }

    [Fact]
    public void GetStringProperty_ShouldReturnNull_WhenPropertyNotFound()
    {
        var myClass = new MyClass();

        var result = myClass.GetStringProperty("NonExistentProperty");

        Assert.Null(result);
    }

    [Fact]
    public void GetStringProperty_ShouldReturnNull_WhenIntermediatePropertyIsNull()
    {
        var myClass = new MyClass();

        var result = myClass.GetStringProperty("Subclass.AnotherString");

        Assert.Null(result);
    }

    [Fact]
    public void GetStringProperty_ShouldReturnNull_WhenNestedPropertyNotString()
    {
        var myClass = new MyClass();
        myClass.Subclass = new MySubclass();
        myClass.Subclass.AnotherString = "World";

        var result = myClass.GetStringProperty("Subclass");

        Assert.Null(result);
    }
}
