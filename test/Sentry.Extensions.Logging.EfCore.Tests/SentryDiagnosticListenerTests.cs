using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Sentry.Internal;
using Sentry.Internal.ScopeStack;
using Xunit;

namespace Sentry.Extensions.Logging.EfCore.Tests
{
    public class SentryDiagnosticListenerTests
    {
        private class Fixture
        {
            private readonly Database _database;

            public IHub Hub { get; set; }
            public ItemsContext Context => new ItemsContext(_database.ContextOptions);

            internal SentryScopeManager ScopeManager { get; }
            public Fixture()
            {
                var options = new SentryOptions();
                ScopeManager = new SentryScopeManager(
                new AsyncLocalScopeStackContainer(),
                options,
                Substitute.For<ISentryClient>()
                );

                Hub = Substitute.For<IHub>();
                Hub.GetSpan().ReturnsForAnyArgs((_) => GetSpan());
                Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<IReadOnlyDictionary<string, object>>())
                    .ReturnsForAnyArgs(callinfo => StartTransaction(Hub, callinfo.Arg<ITransactionContext>()));

                DiagnosticListener.AllListeners.Subscribe(new SentryDiagnosticListener(Hub, options));

                _database = new Database();
                _database.Seed();
            }

            public ISpan GetSpan()
            {
                var (currentScope, _) = ScopeManager.GetCurrent();
                return currentScope.GetSpan();
            }

            public ITransaction StartTransaction(IHub hub, ITransactionContext context)
            {
                var transaction = new TransactionTracer(hub, context);
                transaction.IsSampled = true;
                var (currentScope, _) = ScopeManager.GetCurrent();
                currentScope.Transaction = transaction;
                return transaction;
            }
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void EfCoreIntegration_RunSyncronouQuery_TransactionWithSpans()
        {
            var hub = _fixture.Hub;
            var transaction = hub.StartTransaction("test", "test");
            var spans = transaction.Spans;
            var context = _fixture.Context;

            //Act
            var result = context.Items.FromSqlRaw("SELECT * FROM Items").ToList();

            //Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(2, spans.Count); //1 query compiler, 1 command
            Assert.All(spans, (span) => Assert.True(span.IsFinished));
        }

        [Fact]
        public async Task EfCoreIntegration_RunAsyncQuery_TransactionWithSpansWithOneCompiler()
        {
            var hub = _fixture.Hub;
            var transaction = hub.StartTransaction("test", "test");
            var spans = transaction.Spans;
            var context = _fixture.Context;

            //Act
            var result = new[]
            {
                context.Items.FromSqlRaw("SELECT * FROM Items").ToListAsync(),
                context.Items.FromSqlRaw("SELECT * FROM Items").ToListAsync(),
                context.Items.FromSqlRaw("SELECT * FROM Items").ToListAsync(),
                context.Items.FromSqlRaw("SELECT * FROM Items").ToListAsync(),
            };
            await Task.WhenAll(result);

            //Assert
            Assert.Equal(3, result[0].Result.Count);
            Assert.Equal(5, spans.Count); //1 query compiler, 4 command (same raw sql so it'll only compile once
            Assert.All(spans, (span) => Assert.True(span.IsFinished));
        }

        [Fact]
        public async Task EfCoreIntegration_RunAsyncQuery_TransactionWithSpans()
        {
            var hub = _fixture.Hub;
            var transaction = hub.StartTransaction("test", "test");
            var spans = transaction.Spans;
            var context = _fixture.Context;

            //Act
            var result = new[]
            {
                context.Items.FromSqlRaw("SELECT * FROM Items LIMIT 10").ToListAsync(),
                context.Items.FromSqlRaw("SELECT * FROM Items LIMIT 9").ToListAsync(),
                context.Items.FromSqlRaw("SELECT * FROM Items LIMIT 8").ToListAsync(),
                context.Items.FromSqlRaw("SELECT * FROM Items LIMIT 7").ToListAsync(),
            };
            await Task.WhenAll(result);

            //Assert
            Assert.Equal(3, result[0].Result.Count);
            Assert.Equal(8, spans.Count); //4 query compiler, 4 command (same raw sql so it'll only compile once
            Assert.All(spans, (span) => Assert.True(span.IsFinished));
        }
    }
}
