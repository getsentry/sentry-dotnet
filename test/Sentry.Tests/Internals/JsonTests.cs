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
        private class Fixture
        {
            public string ToJsonString(object @object)
            {
                using var stream = new MemoryStream();
                var writer = new Utf8JsonWriter(stream);
                writer.WriteDynamicValue(@object);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

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
            public DataWithSerializableObject(T obj) : base(obj) { }
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void WriteDynamicValue_ExceptionParameter_NullOutput()
        {
            //Assert
            var ex = new Exception();

            //Act
            var serializedString = _fixture.ToJsonString(ex);

            //Assert
            Assert.Equal("null", serializedString);
        }

        [Fact]
        public void WriteDynamicValue_ClassWithExceptionParameter_SerializedClassWithNullException()
        {
            //Assert
            var expectedSerializedData = "{\"Id\":1,\"Data\":\"1234\",\"Object\":null}";
            var data = new DataAndNonSerializableObject<Exception>(new Exception("my error"));

            //Act
            var serializedString = _fixture.ToJsonString(data);

            //Assert
            Assert.Equal(expectedSerializedData, serializedString);
        }

        [Fact]
        public void WriteDynamicValue_TypeParameter_NullOutput()
        {
            //Assert
            var expectedSerializedData = "{\"Id\":1,\"Data\":\"1234\",\"Object\":null}";
            var data = new DataAndNonSerializableObject<Type>(typeof(List<>).GetGenericArguments()[0]);

            //Act
            var serializedString = _fixture.ToJsonString(data);

            //Assert
            Assert.Equal(expectedSerializedData, serializedString);
        }

        [Fact]
        public void WriteDynamicValue_ClassWithAssembly_SerializedClassWithNullAssembly()
        {
            //Assert
            var expectedSerializedData = "{\"Id\":1,\"Data\":\"1234\",\"Object\":null}";
            var data = new DataAndNonSerializableObject<Assembly>(AppDomain.CurrentDomain.GetAssemblies().First());

            //Act
            var serializedString = _fixture.ToJsonString(data);

            //Assert
            Assert.Equal(expectedSerializedData, serializedString);
        }

        [Fact]
        public void WriteDynamicValue_ClassWithTimeZone_SerializedClassWithTimeZoneInfo()
        {
            //Assert
            var timeZone = TimeZoneInfo.CreateCustomTimeZone(
            "tz_id",
                TimeSpan.FromHours(2),
                "my timezone",
                "my timezone"
            );
            var expectedSerializedData = new[]
            {
                "\"Id\":tz_id,\"Data\":\"1234\"",
                "\"DisplayName\":\"my timezone\"",
                "\"StandardName\":\"my timezone\"",
                "\"BaseUtcOffset\":{\"Ticks\":72000000000,\"Days\":0,\"Hours\":2,\"Milliseconds\":0,\"Minutes\":0,\"Seconds\":0,\"TotalDays\":0.08333333333333333,\"TotalHours\":2,\"TotalMilliseconds\":7200000,\"TotalMinutes\":120,\"TotalSeconds\":7200},",
            };
            var data = new DataWithSerializableObject<TimeZoneInfo>(timeZone);

            //Act
            var serializedString = _fixture.ToJsonString(data);

            //Assert
            Assert.All(expectedSerializedData, p => p.Contains(serializedString));
        }
    }
}
