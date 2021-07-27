using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Sentry.Extensions.Logging.EfCore.Tests
{
    public class SentryDiagnosticListenerTests
    {
        private class Fixture
        {
            private readonly Database _database;

            public ItemsContext Context => new ItemsContext(_database.ContextOptions);

            public TransactionTracer Tracer { get; }

            public IReadOnlyCollection<ISpan> Spans => Tracer?.Spans;
            public IHub Hub { get; set; }
            public Fixture()
            {                
                Hub = Substitute.For<IHub>();
                Tracer = new TransactionTracer(Hub, "foo", "bar");
                Hub.GetSpan().ReturnsForAnyArgs((_) => Spans?.LastOrDefault(s => !s.IsFinished) ?? Tracer);

                DiagnosticListener.AllListeners.Subscribe(new SentryDiagnosticListener(Hub, new SentryOptions()));

                _database = new Database();
                _database.Seed();
            }
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void SyncronousQuery_DoSomething()
        {
            var hub = _fixture.Hub;
            hub.StartTransaction("test", "test");
            var context = _fixture.Context;
            var spans = _fixture.Tracer.Spans;

            //Act
            var result = context.Items.FromSqlRaw("SELECT * FROM Items").ToList();

            //Assert
            Assert.Equal(6, result.Count);
            Assert.Equal(3, spans.Count); //1 command, 1 connection, 1 performance
            Assert.All(spans, (span) => Assert.True(span.IsFinished));
        }
    }
}
