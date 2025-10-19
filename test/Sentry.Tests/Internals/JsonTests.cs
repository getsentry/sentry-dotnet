namespace Sentry.Tests.Internals;

public class JsonTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public JsonTests(ITestOutputHelper output)
    {
        _testOutputLogger = Substitute.ForPartsOf<TestOutputDiagnosticLogger>(output);
#if NET6_0_OR_GREATER
        JsonExtensions.AddJsonSerializerContext(o => new JsonTestsJsonContext(o));
#endif
    }

    public static Exception GenerateException(string description)
    {
        try
        {
            throw new AccessViolationException(description);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    internal class DataAndNonSerializableObject<T>
    {
        /// <summary>
        /// A class containing two objects that can be serialized and a third one that will have issues if serialized.
        /// </summary>
        /// <param name="obj">A problematic or dangerous data</param>
        /// <param name="id">An integer.</param>
        /// <param name="data">a String.</param>
        public DataAndNonSerializableObject(T obj, int id = 1, string data = "1234")
        {
            Id = id;
            Data = data;
            Object = obj;
        }

        public int Id { get; set; }
        public string Data { get; set; }
        public T Object { get; set; }
    }

    private class ExceptionMock
    {
        public int Id { get; set; }
        public string Data { get; set; }
        public ExceptionObjectMock Object { get; set; }
    }

    private class ExceptionObjectMock
    {
        public object TargetSite { get; set; }
        public string StackTrace { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public object InnerException { get; set; }
        public string HelpLink { get; set; }
        public string Source { get; set; }
        public int? HResult { get; set; }
    }

    internal class DataWithSerializableObject<T> : DataAndNonSerializableObject<T>
    {
        /// <summary>
        /// A class containing three objects that can be serialized.
        /// </summary>
        /// <param name="obj">The object.</param>
        public DataWithSerializableObject(T obj) : base(obj) { }
    }

    [Fact]
    public void WriteDynamicValue_ExceptionParameter_SerializedException()
    {
        // Arrange
        var ex = GenerateException("Test");
        ex.Data.Add("a", "b");

        // Act
        var serializedString = ex.ToJsonString(_testOutputLogger);

        // Assert
        var expectedStackTraceString = ex.StackTrace.ToJsonString();
        var expectedSerializedData = new[]
        {
            """
            "Message":"Test"
            """,
            """
            "Data":{"a":"b"}
            """,
            """
            "InnerException":null
            """,
            """
            "Source":"Sentry.Tests"
            """,
            $"""
            "StackTrace":{expectedStackTraceString}
            """
        };

        Assert.All(expectedSerializedData, expected => Assert.Contains(expected, serializedString));
    }

    [Fact]
    public void WriteDynamicValue_ClassWithExceptionParameter_SerializedClassWithException()
    {
        // Arrange
        var expectedMessage = "T est";
        var expectedData = new KeyValuePair<string, string>("a", "b");
        var ex = GenerateException(expectedMessage);
        ex.Data.Add(expectedData.Key, expectedData.Value);
        var data = new DataWithSerializableObject<Exception>(ex);

        // Act
        var serializedString = data.ToJsonString(_testOutputLogger);
        var exceptionDeserialized = JsonSerializer.Deserialize<ExceptionMock>(serializedString);

        // Assert
        Assert.NotNull(exceptionDeserialized);
        Assert.Equal(1, exceptionDeserialized.Id);
        Assert.Equal("1234", exceptionDeserialized.Data);
        Assert.NotNull(exceptionDeserialized.Object.StackTrace);
        Assert.Equal(ex.StackTrace, exceptionDeserialized.Object.StackTrace);
        Assert.Null(exceptionDeserialized.Object.TargetSite);
        Assert.Equal(expectedMessage, exceptionDeserialized.Object.Message);
        Assert.Contains(expectedData, exceptionDeserialized.Object.Data);
        Assert.Null(exceptionDeserialized.Object.InnerException);
        Assert.Null(exceptionDeserialized.Object.HelpLink);
        Assert.Equal(ex.Source, exceptionDeserialized.Object.Source);
        Assert.Equal(ex.HResult, exceptionDeserialized.Object.HResult);
    }

    [Fact]
    public void WriteDynamicValue_TypeParameter_FullNameTypeOutput()
    {
        // Arrange
        var type = typeof(Exception);
        var expectedValue = "\"System.Exception\"";

        // Act
        var serializedString = type.ToJsonString(_testOutputLogger);

        // Assert
        Assert.Equal(expectedValue, serializedString);
    }

    [Fact]
    public void WriteDynamicValue_ClassWithTypeParameter_ClassFormatted()
    {
        // Arrange
        var type = typeof(List<>).GetGenericArguments()[0];
        var data = new DataWithSerializableObject<Type>(type);

        // Act
        var serializedString = data.ToJsonString(_testOutputLogger);

        // Assert
        const string expected = """{"Id":1,"Data":"1234","Object":null}""";
        Assert.Equal(expected, serializedString);
    }

    [Fact]
    public void WriteDynamicValue_ClassWithAssembly_SerializedClassWithNullAssembly()
    {
        // Arrange
        var data = new DataAndNonSerializableObject<Assembly>(AppDomain.CurrentDomain.GetAssemblies()[0]);

        // Act
        var serializedString = data.ToJsonString(_testOutputLogger);

        // Assert
        const string expected = """{"Id":1,"Data":"1234","Object":null}""";
        Assert.Equal(expected, serializedString);
    }

    [Theory]
    [MemberData(nameof(NonSerializableObjectTestData))]
    public void WriteDynamic_NonSerializableObject_LogException(object testObject)
    {
        // Arrange
        JsonExtensions.JsonPreserveReferences = false;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            // Act
            writer.WriteDynamic("property_name", testObject, _testOutputLogger);

            writer.WriteEndObject();
        }

        // Assert
        _testOutputLogger.Received(1).Log(
            Arg.Is(SentryLevel.Error),
            "Failed to serialize object for property '{0}'. Original depth: {1}, current depth: {2}",
            Arg.Any<Exception>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public void WriteDynamic_ComplexObject_PreserveReferences()
    {
        // Arrange
        JsonExtensions.JsonPreserveReferences = true;
        var testObject = new SelfReferencedObject();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            // Act
            writer.WriteDynamic("property_name", testObject, _testOutputLogger);

            writer.WriteEndObject();
        }

        var json = Encoding.UTF8.GetString(stream.ToArray());

        // Assert
        Assert.Equal("""{"property_name":{"$id":"1","Object":{"$ref":"1"}}}""", json);
    }

    public static IEnumerable<object[]> NonSerializableObjectTestData =>
        new[]
        {
            new object[] {new NonSerializableObject()},
            new object[] {new SelfReferencedObject()},
        };

    public class NonSerializableObject
    {
#pragma warning disable CA1822 // Mark members as static
        public string Thrower => throw new InvalidDataException();
#pragma warning restore CA1822
    }

    public class SelfReferencedObject
    {
        public SelfReferencedObject Object => this;
    }
}

#if NET6_0_OR_GREATER
[JsonSerializable(typeof(AccessViolationException))]
[JsonSerializable(typeof(Exception))]
[JsonSerializable(typeof(JsonTests.DataAndNonSerializableObject<Assembly>))]
[JsonSerializable(typeof(JsonTests.DataWithSerializableObject<Exception>))]
[JsonSerializable(typeof(JsonTests.SelfReferencedObject))]
[JsonSerializable(typeof(JsonTests.DataWithSerializableObject<Type>))]
internal partial class JsonTestsJsonContext : JsonSerializerContext
{
}
#endif
