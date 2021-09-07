using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Sentry.Internal;
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
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteDynamicValue(@object);
                }
                return Encoding.UTF8.GetString(stream.ToArray());
            }

            public Exception GenerateException(string description)
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

            public SentryJsonConverter GetConverter() => new SentryJsonConverter();
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
        public void WriteDynamicValue_ExceptionParameter_SerializedException()
        {
            //Assert
            var expectedMessage = "T est";
            var expectedData = new KeyValuePair<string, string>("a", "b");
            var ex = _fixture.GenerateException(expectedMessage);
            ex.Data.Add(expectedData.Key, expectedData.Value);
            var expectedStackTrace = _fixture.ToJsonString(ex.StackTrace);
            var expectedSerializedData = new[]
            {
                $"\"Message\":\"{expectedMessage}\"",
                "\"Data\":{\"" + expectedData.Key + "\":\"" + expectedData.Value + "\"}",
                "\"InnerException\":null",
                "\"Source\":\"Sentry.Tests\"",
                $"\"StackTrace\":{expectedStackTrace}"
            };

            //Act
            var serializedString = _fixture.ToJsonString(ex);

            //Assert
            Assert.All(expectedSerializedData, expectedData => Assert.Contains(expectedData, serializedString));
        }

        [Fact]
        public void WriteDynamicValue_ClassWithExceptionParameter_SerializedClassWithException()
        {
            //Assert
            var expectedMessage = "T est";
            var expectedData = new KeyValuePair<string, string>("a", "b");
            var ex = _fixture.GenerateException(expectedMessage);
            var expectedStackTrace = _fixture.ToJsonString(ex.StackTrace);
            ex.Data.Add(expectedData.Key, expectedData.Value);

            var expectedSerializedData =
                "{\"" +
                "Id\":1," +
                "\"Data\":\"1234\"" +
                ",\"Object\":{";

            var expectedSerializedException = new[]
            {
                $"\"Message\":\"{expectedMessage}\"",
                "\"Data\":{\"" + expectedData.Key + "\":\"" + expectedData.Value + "\"}",
                "\"InnerException\":null",
                "\"Source\":\"Sentry.Tests\"",
                $"\"StackTrace\":{expectedStackTrace}"
            };
            var data = new DataWithSerializableObject<Exception>(ex);

            //Act
            var serializedString = _fixture.ToJsonString(data);

            //Assert
            Assert.StartsWith(expectedSerializedData, serializedString);
            Assert.All(expectedSerializedData, expectedData => Assert.Contains(expectedData, serializedString));
        }

        [Fact]
        public void WriteDynamicValue_TypeParameter_FullNameTypeOutput()
        {
            //Assert
            var type = typeof(Exception);
            var expectedValue = "\"System.Exception\"";

            //Act
            var serializedString = _fixture.ToJsonString(type);

            //Assert
            Assert.Equal(expectedValue, serializedString);
        }

        [Fact]
        public void WriteDynamicValue_ClassWithTypeParameter_ClassFormatted()
        {
            //Assert
            var type = typeof(List<>).GetGenericArguments()[0];
            var data = new DataWithSerializableObject<Type>(type);
            var expectedSerializedData =
                "{" +
                "\"Id\":1," +
                "\"Data\":\"1234\"," +
                $"\"Object\":null" + //This type has no Full Name.
                "}";

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
            var data = new DataAndNonSerializableObject<Assembly>(AppDomain.CurrentDomain.GetAssemblies()[0]);

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
                "my timezone");
            var expectedSerializedData = new[]
            {
                "\"Id\":1,\"Data\":\"1234\"",
                "\"Id\":\"tz_id\"",
                "\"DisplayName\":\"my timezone\"",
                "\"StandardName\":\"my timezone\"",
                "\"BaseUtcOffset\":{\"Ticks\":72000000000,\"Days\":0,\"Hours\":2,\"Milliseconds\":0,\"Minutes\":0,\"Seconds\":0",
                "\"TotalHours\":2,\"TotalMilliseconds\":7200000,\"TotalMinutes\":120,\"TotalSeconds\":7200},",
            };
            var data = new DataWithSerializableObject<TimeZoneInfo>(timeZone);

            //Act
            var serializedString = _fixture.ToJsonString(data);

            //Assert
            Assert.All(expectedSerializedData, expectedData => Assert.Contains(expectedData, serializedString));
        }
    }
}
