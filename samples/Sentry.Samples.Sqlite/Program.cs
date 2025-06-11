using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Sentry.Sqlite;

SentrySdk.Init(options =>
{
#if !SENTRY_DSN_DEFINED_IN_ENV
    // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    options.Dsn = SamplesShared.Dsn;
#endif
    options.Debug = false;
    options.TracesSampleRate = 1.0;
});

using (var connection = new SentrySqliteConnection("Data Source=hello.db"))
{
    connection.Open();

    var command = connection.CreateCommand();
    command.CommandText =
    @"
        CREATE TABLE user (
            id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL
        );

        INSERT INTO user
        VALUES (1, 'Brice'),
               (2, 'Alexander'),
               (3, 'Nate');
    ";
    command.ExecuteNonQuery();

    Console.Write("Name: ");
    var name = Console.ReadLine();

    #region snippet_Parameter
    command.CommandText =
    @"
        INSERT INTO user (name)
        VALUES ($name)
    ";
    command.Parameters.AddWithValue("$name", name);
    #endregion
    command.ExecuteNonQuery();

    command.CommandText =
    @"
        SELECT last_insert_rowid()
    ";
    var newId = (long)command.ExecuteScalar()!;

    Console.WriteLine($"Your new user ID is {newId}.");
}

Console.Write("User ID: ");
var id = int.Parse(Console.ReadLine()!);

#region snippet_HelloWorld
using (var connection = new SqliteConnection("Data Source=hello.db"))
{
    connection.Open();

    var command = connection.CreateCommand();
    command.CommandText =
    @"
        SELECT name
        FROM user
        WHERE id = $id
    ";
    command.Parameters.AddWithValue("$id", id);

    using (var reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            var name = reader.GetString(0);

            Console.WriteLine($"Hello, {name}!");
        }
    }
}
#endregion

// Clean up
SqliteConnection.ClearAllPools();
File.Delete("hello.db");
