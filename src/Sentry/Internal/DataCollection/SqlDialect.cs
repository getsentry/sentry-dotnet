namespace Sentry.Internal.DataCollection;

internal enum SqlDialect
{
    Generic,  // Postgres compatible
    SqlServer,
    MySql,
    Sqlite
}
