using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sentry.EntityFramework.ErrorProcessors;
using Sentry.Extensibility;
using Sentry.Protocol;
using Xunit;

namespace Sentry.EntityFramework.Tests
{
    public class ErrorProcessorTests
    {
        private class Fixture
        {
            public DbConnection DbConnection { get; }
            public TestDbContext DbContext { get; }

            public ISentryClient SentryClient;

            public Func<SentryEvent, SentryEvent> BeforeSend = null;

            private SentryEvent _beforeSend(SentryEvent arg)
            {
                if (BeforeSend != null)
                {
                    return BeforeSend(arg);
                }
                return arg;
            }

            public Fixture()
            {
                DbConnection = Effort.DbConnectionFactory.CreateTransient();
                DbContext = new TestDbContext(DbConnection, true);
                SentryClient = new SentryClient(new SentryOptions()
                {
                    BeforeSend = _beforeSend,
                    Dsn = DsnSamples.Valid,
                }.AddEntityFramework());
            }
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public async Task EntityValidationExceptions_Extra_EntityValidationErrorsNotNullAsync()
        {
            // We use an actual Entity Framework instane since manually generating any EF related data is highly inaccurate
            _fixture.DbContext.TestTable.Add(new TestDbContext.TestData());
            try
            {
                // This will throw a validation exception since TestData has a Required column which we didn't set
                await _fixture.DbContext.SaveChangesAsync();
            }
            catch(DbEntityValidationException e)
            {
                // SaveChanges will throw an exception

                var processor = new DbEntityValidationExceptionProcessor();
                var evt = new SentryEvent();
                processor.Process(e, evt);

                Assert.True(evt.Extra.TryGetValue("EntityValidationErrors", out var errors));
                var entityValidationErrors = errors as Dictionary<string, List<string>>;
                Assert.NotNull(entityValidationErrors);
                Assert.NotEmpty(entityValidationErrors);
            }
            
        }

        /// <summary>
        /// Integration test to ensure that the processor is also called and operated successfully inside an actual Sentry Client
        /// This should help avoid regression in case the underlying API changes in an unusual way
        /// </summary>
        [Fact]
        public async Task Integration_DbEntityValidationExceptionProcessorAsync()
        {
            // We use an actual Entity Framework instane since manually generating any EF related data is highly inaccurate
            _fixture.DbContext.TestTable.Add(new TestDbContext.TestData());
            try
            {
                // This will throw a validation exception since TestData has a Required column which we didn't set
                await _fixture.DbContext.SaveChangesAsync();
            }
            catch(DbEntityValidationException e)
            {
                Exception assertError = null;
                // SaveChanges will throw an exception
                _fixture.BeforeSend = evt =>
                {
                    // We use a try-catch here as we cannot assert directly since SentryClient itself would catch the thrown assertion errors
                    try
                    {
                        Assert.True(evt.Extra.TryGetValue("EntityValidationErrors", out var errors));
                        var entityValidationErrors = errors as Dictionary<string, List<string>>;
                        Assert.NotNull(entityValidationErrors);
                        Assert.NotEmpty(entityValidationErrors);
                    }
                    catch (Exception ex)
                    {
                        assertError = ex;
                    }
                    
                    return null;
                };
                _fixture.SentryClient.CaptureException(e);
                Assert.Null(assertError);
            }
        }
    }
}
