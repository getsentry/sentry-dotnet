using Sentry.Internal.DataCollection;

namespace Sentry.Tests.Internals.DataCollection;

public class SqlSanitizerTests
{
    [Theory]
    [InlineData("SELECT * FROM users WHERE email = 'jane@example.com'", "SELECT * FROM users WHERE email = ?")]
    [InlineData("SELECT * FROM users WHERE name = 'O''Brien' AND id = 1", "SELECT * FROM users WHERE name = ? AND id = ?")]
    [InlineData("SELECT '' FROM t", "SELECT ? FROM t")]
    [InlineData("SELECT 'a', 'b' FROM t", "SELECT ?, ? FROM t")]
    [InlineData("SELECT 'C:\\' FROM t WHERE x = 'y'", "SELECT ? FROM t WHERE x = ?")]
    [InlineData("SELECT 'multi\nline' FROM t", "SELECT ? FROM t")]
    [InlineData("SELECT N'jane' FROM t", "SELECT ? FROM t")]
    [InlineData("SELECT n'jane' FROM t", "SELECT ? FROM t")]
    [InlineData("SELECT X'DEADBEEF', B'0101' FROM t", "SELECT ?, ? FROM t")]
    [InlineData("SELECT E'it\\'s' FROM t WHERE x = 1", "SELECT ? FROM t WHERE x = ?")]
    [InlineData("SELECT $$it's$$ FROM t", "SELECT ? FROM t")]
    [InlineData("SELECT $tag$a $$ b$tag$ FROM t", "SELECT ? FROM t")]
    public void Sanitize_StringLiterals_AreReplaced(string sql, string expected) =>
        SqlSanitizer.Sanitize(sql).Should().Be(expected);

    [Theory]
    [InlineData("SELECT 42", "SELECT ?")]
    [InlineData("SELECT -42", "SELECT -?")]
    [InlineData("SELECT 3.14, .5, 1e10, 1e5, 1.5E-3, 2e+8", "SELECT ?, ?, ?, ?, ?, ?")]
    [InlineData("SELECT 9, 1999, 0.99", "SELECT ?, ?, ?")]
    [InlineData(".5", "?")]
    [InlineData("SELECT x.5", "SELECT x.?")]
    [InlineData("SELECT 1_000_000", "SELECT ?")]
    [InlineData("SELECT 0xDEADBEEF, 0X1f, 0xabcdef, 0b1010, 0o17", "SELECT ?, ?, ?, ?, ?")]
    [InlineData("SELECT * FROM t LIMIT 10 OFFSET 20", "SELECT * FROM t LIMIT ? OFFSET ?")]
    [InlineData("SELECT * FROM t WHERE id IN (1, 2, 3)", "SELECT * FROM t WHERE id IN (?, ?, ?)")]
    [InlineData("SELECT CHAR(13)", "SELECT CHAR(?)")]
    public void Sanitize_NumericLiterals_AreReplaced(string sql, string expected) =>
        SqlSanitizer.Sanitize(sql).Should().Be(expected);

    [Theory]
    [InlineData("SELECT 1 -- jane@example.com\nFROM t", "SELECT ? \nFROM t")]
    [InlineData("SELECT 1 -- trailing", "SELECT ?")]
    [InlineData("-- TagWith: jane\n\nSELECT 1", "SELECT ?")]
    [InlineData("\r\n-- GetOrders\r\n-- File: Orders.cs:42\r\n\r\nSELECT 1", "SELECT ?")]
    [InlineData("/* jane */\nSELECT 1", "SELECT ?")]
    [InlineData("SELECT /* jane */ 1", "SELECT  ?")]
    [InlineData("SELECT/* jane */1", "SELECT ?")]
    [InlineData("SELECT /* jane */1", "SELECT ?")]
    [InlineData("SELECT/* jane */ 1", "SELECT ?")]
    [InlineData("/* app */SELECT 1", "SELECT ?")]
    [InlineData("SELECT 1/* jane */", "SELECT ?")]
    [InlineData("SELECT /*+ INDEX(t idx) */ * FROM t", "SELECT  * FROM t")]
    [InlineData("SELECT /* outer /* inner */ still a comment */ 1", "SELECT  ?")]
    [InlineData("SELECT 1 /* unterminated 'jane'", "SELECT ?")]
    [InlineData("SELECT '--not a comment', '/*nor this*/' FROM t", "SELECT ?, ? FROM t")]
    public void Sanitize_Comments_AreRemoved(string sql, string expected) =>
        SqlSanitizer.Sanitize(sql).Should().Be(expected);

    [Fact]
    public void Sanitize_MySqlHashCommentBeforeTheStatement_IsRemoved() =>
        SqlSanitizer.Sanitize("# jane\nSELECT 1", "mysql").Should().Be("SELECT ?");

    [Theory]
    [InlineData("SELECT \"u\".\"Id\", \"u\".\"Name\" FROM \"Users\" AS \"u\"")]
    [InlineData("SELECT \"col \"\"1\"\"\" FROM t")]
    [InlineData("SELECT `u`.`Id` FROM `Users` AS `u`")]
    [InlineData("SELECT t1.col2, x9 FROM table3 AS t1")]
    [InlineData("SELECT * FROM #temp JOIN ##global ON #temp.id = ##global.id")]
    [InlineData("SELECT * FROM t WHERE a = @p0 AND b = @__name_1 AND c = @@ROWCOUNT")]
    [InlineData("SELECT * FROM t WHERE a = :name AND b = ?")]
    [InlineData("SELECT * FROM t WHERE a = $1 AND b = $2::int")]
    [InlineData("OUTPUT $action, inserted.$IDENTITY")]
    [InlineData("SELECT data->>'key' FROM t")]
    public void Sanitize_IdentifiersAndPlaceholders_AreLeftAlone(string sql)
    {
        var expected = sql.Replace("'key'", "?");

        SqlSanitizer.Sanitize(sql).Should().Be(expected);
    }

    [Theory]
    [InlineData("mssql", "SELECT \"x\", [y 2] FROM t")]
    [InlineData("mssqlcompact", "SELECT \"x\", [y 2] FROM t")]
    [InlineData("sqlite", "SELECT \"x\", [y 2] FROM t")]
    [InlineData("mysql", "SELECT ?, [y ?] FROM t")]
    [InlineData("spanner", "SELECT ?, [y ?] FROM t")]
    [InlineData("postgresql", "SELECT \"x\", [y ?] FROM t")]
    [InlineData("oracle", "SELECT \"x\", [y ?] FROM t")]
    [InlineData(null, "SELECT \"x\", [y ?] FROM t")]
    public void Sanitize_DbSystem_PicksItsRules(string dbSystem, string expected) =>
        SqlSanitizer.Sanitize("SELECT \"x\", [y 2] FROM t", dbSystem).Should().Be(expected);

    [Theory]
    [InlineData("mssql")]
    [InlineData("sqlite")]
    public void Sanitize_SqlServerAndSqliteBrackets_AreIdentifiers(string dbSystem)
    {
        const string sql = "SELECT [Order Details 2].[Unit's Price] FROM [dbo].[t]]1]";

        SqlSanitizer.Sanitize(sql, dbSystem).Should().Be(sql);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("mysql")]
    public void Sanitize_OtherDialectBrackets_AreNotIdentifiers(string dbSystem) =>
        SqlSanitizer.Sanitize("SELECT ARRAY['jane'], tags[1] FROM t", dbSystem)
            .Should().Be("SELECT ARRAY[?], tags[?] FROM t");

    [Theory]
    [InlineData("SELECT @5, @ -5 FROM t", "SELECT @?, @ -? FROM t")]
    [InlineData("SELECT 5 #3, 5#3, x#5 FROM t", "SELECT ? #?, ?#?, x#? FROM t")]
    [InlineData("SELECT * FROM t WHERE a@1 = b", "SELECT * FROM t WHERE a@? = b")]
    public void Sanitize_PostgreSqlOperatorsBeforeDigits_ReplaceTheNumber(string sql, string expected) =>
        SqlSanitizer.Sanitize(sql).Should().Be(expected);

    [Fact]
    public void Sanitize_SqlServerAtAndHashBeforeDigits_AreIdentifiers() =>
        SqlSanitizer.Sanitize("SELECT * FROM #1 JOIN t#2 ON @1 = 1", "mssql")
            .Should().Be("SELECT * FROM #1 JOIN t#2 ON @1 = ?");

    [Fact]
    public void Sanitize_SqlServerMoneyLiteral_IsReplaced() =>
        SqlSanitizer.Sanitize("SELECT $12.50, $.5 FROM t", "mssql")
            .Should().Be("SELECT ?, $.? FROM t");

    [Fact]
    public void Sanitize_SqlServerEscapeStringPrefix_IsAWord() =>
        SqlSanitizer.Sanitize("a = 'zq' AND b = 1E'zq\\' OR c = 'zq2'", "mssql")
            .Should().Be("a = ? AND b = ?E? OR c = ?");

    [Theory]
    [InlineData("SELECT * FROM t WHERE name = \"jane\"", "SELECT * FROM t WHERE name = ?")]
    [InlineData("SELECT * FROM t WHERE name = 'it\\'s' AND x = 1", "SELECT * FROM t WHERE name = ? AND x = ?")]
    [InlineData("SELECT * FROM t WHERE name = N'it\\'s' AND x = 1", "SELECT * FROM t WHERE name = ? AND x = ?")]
    [InlineData("SELECT * FROM t WHERE name = \"say \\\"hi\\\"\" AND x = 1", "SELECT * FROM t WHERE name = ? AND x = ?")]
    [InlineData("SELECT 1 # jane@example.com\nFROM t", "SELECT ? \nFROM t")]
    [InlineData("SELECT a# jane\nFROM t", "SELECT a\nFROM t")]
    [InlineData("SELECT `Id` FROM `Users`", "SELECT `Id` FROM `Users`")]
    public void Sanitize_MySql_UsesMySqlQuoting(string sql, string expected) =>
        SqlSanitizer.Sanitize(sql, "mysql").Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("mssql")]
    [InlineData("sqlite")]
    public void Sanitize_DoubleQuotesOutsideMySql_AreIdentifiers(string dbSystem) =>
        SqlSanitizer.Sanitize("SELECT \"Name\" FROM \"Users\" WHERE \"Id\" = 1", dbSystem)
            .Should().Be("SELECT \"Name\" FROM \"Users\" WHERE \"Id\" = ?");

    [Theory]
    [InlineData("SELECT * FROM t WHERE name = 'jane", "SELECT * FROM t WHERE name = ?")]
    [InlineData("SELECT * FROM t WHERE name = 'jane'' AND id = 1", "SELECT * FROM t WHERE name = ?")]
    [InlineData("SELECT * FROM t WHERE name = $$jane", "SELECT * FROM t WHERE name = ?")]
    [InlineData("SELECT * FROM t WHERE name = $tag$jane$other$", "SELECT * FROM t WHERE name = ?")]
    [InlineData("SELECT * FROM \"t WHERE name = 'jane'", "SELECT * FROM ?")]
    [InlineData("SELECT * FROM `t WHERE name = 'jane'", "SELECT * FROM ?")]
    [InlineData("SELECT * FROM t WHERE name = E'jane\\'", "SELECT * FROM t WHERE name = ?")]
    public void Sanitize_UnterminatedLiteral_ReplacesTheRest(string sql, string expected) =>
        SqlSanitizer.Sanitize(sql).Should().Be(expected);

    [Fact]
    public void Sanitize_UnterminatedDialectLiteral_ReplacesTheRest()
    {
        using (new AssertionScope())
        {
            SqlSanitizer.Sanitize("SELECT * FROM [t WHERE name = 'jane'", "mssql")
                .Should().Be("SELECT * FROM ?");
            SqlSanitizer.Sanitize("SELECT * FROM t WHERE name = \"jane\\\"", "mysql")
                .Should().Be("SELECT * FROM t WHERE name = ?");
        }
    }

    [Fact]
    public void Sanitize_EfCoreSqliteCommand_KeepsStructure()
    {
        const string sql = """
            SELECT "i"."Id", "i"."Name"
            FROM "Items" AS "i"
            WHERE "i"."Name" = 'jane@example.com' AND "i"."Id" > 42 AND "i"."Name" = @captured
            LIMIT @p0 OFFSET @p
            """;

        SqlSanitizer.Sanitize(sql, "sqlite").Should().Be("""
            SELECT "i"."Id", "i"."Name"
            FROM "Items" AS "i"
            WHERE "i"."Name" = ? AND "i"."Id" > ? AND "i"."Name" = @captured
            LIMIT @p0 OFFSET @p
            """);
    }

    [Fact]
    public void Sanitize_Null_ReturnsNull() =>
        SqlSanitizer.Sanitize(null).Should().BeNull();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    [InlineData("/* only a comment */")]
    [InlineData("-- only a comment")]
    public void Sanitize_EmptyOrWhitespace_ReturnsEmpty(string sql) =>
        SqlSanitizer.Sanitize(sql).Should().BeEmpty();

    [Fact]
    public void Sanitize_StatementWithoutLiterals_IsUnchanged()
    {
        const string sql = "UPDATE [Users] SET [Name] = @p0 WHERE [Id] = @p1;\nSELECT @@ROWCOUNT;";

        SqlSanitizer.Sanitize(sql, "mssql").Should().Be(sql);
    }
}
