using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using Sentry.EntityFramework.ErrorProcessors;
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
            public IHub Hub { get; set; } = Substitute.For<IHub>();
            public Scope Scope { get; } = new Scope(new SentryOptions().AddEntityFramework());
            public Fixture()
            {
                DbConnection = Effort.DbConnectionFactory.CreateTransient();
                DbContext = new TestDbContext(DbConnection, true);
                Hub.IsEnabled.Returns(true);
                Hub.ConfigureScope(Arg.Invoke(Scope));
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
    }
}
