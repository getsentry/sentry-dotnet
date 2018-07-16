using System;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using Effort.Provider;
using NSubstitute;
using Sentry.Protocol;
using Sentry.Testing;
using Xunit;

namespace Sentry.EntityFramework.Tests
{
    public class SentryCommandInterceptorTests
    {
        private class Fixture
        {
            public DbConnection DbConnection { get; }
            public TestDbContext DbContext { get; }
            public IQueryLogger QueryLogger { get; } = Substitute.For<IQueryLogger>();
            public Fixture()
            {
                DbConnection = Effort.DbConnectionFactory.CreateTransient();
                DbContext = new TestDbContext(DbConnection, true);
            }
        }

        private readonly Fixture _fixture = new Fixture();

        [NoMonoFact]
        public void UseBreadCrumbs_SentryDatabaseLogging_AddsInterceptor()
        {
            var interceptor = SentryDatabaseLogging.UseBreadcrumbs(_fixture.QueryLogger);
            Assert.NotNull(interceptor);
        }

        [NoMonoFact]
        public void NonQueryExecuting_SentryCommandInterceptor_CapturesQuery()
        {
            var expected = new
            {
                Query = "Expected query string"
            };

            var interceptor = SentryDatabaseLogging.UseBreadcrumbs(_fixture.QueryLogger);

            var command = new EffortCommand()
            {
                CommandText = expected.Query
            };

            interceptor.NonQueryExecuting(command, new DbCommandInterceptionContext<int>());
            _fixture.QueryLogger.Received(1).Log(expected.Query, BreadcrumbLevel.Debug);
        }

        [NoMonoFact]
        public void NonQueryExecuting_WithException_CapturesQuery()
        {
            var expected = new
            {
                Query = "Expected query string"
            };

            var interceptor = SentryDatabaseLogging.UseBreadcrumbs(_fixture.QueryLogger);

            var command = new EffortCommand()
            {
                CommandText = expected.Query
            };

            interceptor.NonQueryExecuting(command, new DbCommandInterceptionContext<int>() { Exception = new Exception() });
            _fixture.QueryLogger.Received(1).Log(expected.Query, BreadcrumbLevel.Error);
        }

        [NoMonoFact]
        public void ReaderExecuting_SentryCommandInterceptor_CapturesQuery()
        {
            var expected = new
            {
                Query = "Expected query string"
            };

            var interceptor = SentryDatabaseLogging.UseBreadcrumbs(_fixture.QueryLogger);

            var command = new EffortCommand()
            {
                CommandText = expected.Query
            };

            interceptor.ReaderExecuting(command, new DbCommandInterceptionContext<DbDataReader>());
            _fixture.QueryLogger.Received(1).Log(expected.Query, BreadcrumbLevel.Debug);
        }

        [NoMonoFact]
        public void ReaderExecuting_WithException_CapturesQuery()
        {
            var expected = new
            {
                Query = "Expected query string"
            };

            var interceptor = SentryDatabaseLogging.UseBreadcrumbs(_fixture.QueryLogger);

            var command = new EffortCommand()
            {
                CommandText = expected.Query
            };

            interceptor.ReaderExecuting(command, new DbCommandInterceptionContext<DbDataReader>() { Exception = new Exception() });
            _fixture.QueryLogger.Received(1).Log(expected.Query, BreadcrumbLevel.Error);
        }

        [NoMonoFact]
        public void ScalarExecuting_SentryCommandInterceptor_CapturesQuery()
        {
            var expected = new
            {
                Query = "Expected query string"
            };

            var interceptor = SentryDatabaseLogging.UseBreadcrumbs(_fixture.QueryLogger);

            var command = new EffortCommand()
            {
                CommandText = expected.Query
            };

            interceptor.ScalarExecuting(command, new DbCommandInterceptionContext<object>());
            _fixture.QueryLogger.Received(1).Log(expected.Query, BreadcrumbLevel.Debug);
        }

        [NoMonoFact]
        public void ScalarExecuting_WithException_CapturesQuery()
        {
            var expected = new
            {
                Query = "Expected query string"
            };

            var interceptor = SentryDatabaseLogging.UseBreadcrumbs(_fixture.QueryLogger);

            var command = new EffortCommand()
            {
                CommandText = expected.Query
            };

            interceptor.ScalarExecuting(command, new DbCommandInterceptionContext<object>() { Exception = new Exception() });
            _fixture.QueryLogger.Received(1).Log(expected.Query, BreadcrumbLevel.Error);
        }

        [NoMonoFact]
        public void FirstOrDefault_FromDatabase_CapturesQuery()
        {
            _ = SentryDatabaseLogging.UseBreadcrumbs(_fixture.QueryLogger);

            _ = _fixture.DbContext.TestTable.FirstOrDefault();

            _fixture.QueryLogger.Received().Log(Arg.Any<string>(), BreadcrumbLevel.Debug);
        }
    }
}
