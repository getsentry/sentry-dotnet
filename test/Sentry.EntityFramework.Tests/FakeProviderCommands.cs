using System.Data;

namespace Sentry.EntityFramework.Tests
{
    internal abstract class FakeDbCommand : DbCommand
    {
        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
        protected override DbTransaction DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => throw new NotSupportedException();
        public override object ExecuteScalar() => throw new NotSupportedException();
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
    }
}

namespace MySql.Data.MySqlClient
{
    internal class FakeMySqlCommand : Sentry.EntityFramework.Tests.FakeDbCommand;
}
