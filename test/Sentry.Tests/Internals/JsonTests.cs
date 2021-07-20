using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Sentry.Internal.Extensions;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class JsonTests
    {
        private class DataAndNonSerializableObject<T>
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
                @Object = obj;
            }

            public int Id { get; set; }
            public string Data { get; set; }
            public T @Object { get; set; }
        }

        private class DataWithSerializableObject<T> : DataAndNonSerializableObject<T>
        {
            /// <summary>
            /// A class containing three objects that can be serialized.
            /// </summary>
            /// <param name="obj">The object.</param>
            public DataWithSerializableObject(T obj) : base(obj){ }
        }

    [Fact]
    public void WriteDynamicValue_ExceptionParameter_NullOutput()
    {
        //Assert
        using var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream);
        var ex = new Exception();

        //Act
        writer.WriteDynamicValue(ex);

        //Assert
        Assert.Equal("null", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void WriteDynamicValue_ClassWithExceptionParameter_SerializedClassWithNullException()
    {
        //Assert
        using var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream);
        var expectedSerializedData = "{\"Id\":1,\"Data\":\"1234\",\"Object\":null}";
        var data = new DataAndNonSerializableObject<Exception>(new Exception("my error"));

        //Act
        writer.WriteDynamicValue(data);

        //Assert
        Assert.Equal(expectedSerializedData, Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void WriteDynamicValue_TypeParameter_NullOutput()
    {
        //Assert
        using var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream);
        var expectedSerializedData = "{\"Id\":1,\"Data\":\"1234\",\"Object\":null}";
        var data = new DataAndNonSerializableObject<Type>(typeof(List<>).GetGenericArguments()[0]);

        //Act
        writer.WriteDynamicValue(data);

        //Assert
        Assert.Equal(expectedSerializedData, Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void WriteDynamicValue_ClassWithAssembly_SerializedClassWithNullAssembly()
    {
        //Assert
        using var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream);
        var expectedSerializedData = "{\"Id\":1,\"Data\":\"1234\",\"Object\":null}";
        var data = new DataAndNonSerializableObject<Assembly>(AppDomain.CurrentDomain.GetAssemblies().First());

        //Act
        writer.WriteDynamicValue(data);

        //Assert
        Assert.Equal(expectedSerializedData, Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void WriteDynamicValue_ClassWithTimeZone_SerializedClassWithTimeZoneInfo()
    {
        //Assert
        using var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream);
        var expectedSerializedData = "{\"Id\":1,\"Data\":\"1234\",\"Object\":{\"Id\":\"UTC\",\"DisplayName\":\"(UTC) Coordinated Universal Time\",\"StandardName\":\"Coordinated Universal Time\",\"DaylightName\":\"Coordinated Universal Time\",\"BaseUtcOffset\":{\"Ticks\":0,\"Days\":0,\"Hours\":0,\"Milliseconds\":0,\"Minutes\":0,\"Seconds\":0,\"TotalDays\":0,\"TotalHours\":0,\"TotalMilliseconds\":0,\"TotalMinutes\":0,\"TotalSeconds\":0},\"SupportsDaylightSavingTime\":false}}";
        var data = new DataWithSerializableObject<TimeZoneInfo>(TimeZoneInfo.Utc);

        //Act
        writer.WriteDynamicValue(data);
        var x = Encoding.UTF8.GetString(stream.ToArray());

        //Assert
        Assert.Equal(expectedSerializedData, Encoding.UTF8.GetString(stream.ToArray()));
    }
}
}
