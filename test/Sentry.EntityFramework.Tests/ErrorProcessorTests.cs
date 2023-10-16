namespace Sentry.EntityFramework.Tests;

public class ErrorProcessorTests
{
    [Fact]
    public async Task EntityValidationExceptions_Extra_EntityValidationErrorsNotNullAsync()
    {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var context = new TestDbContext(connection, true);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
        };
        options.AddEntityFramework();
        using var client = new SentryClient(options);
        // We use an actual Entity Framework instance since manually generating any EF related data is highly inaccurate
        context.TestTable.Add(new TestDbContext.TestData());
        try
        {
            // This will throw a validation exception since TestData has a Required column which we didn't set
            await context.SaveChangesAsync();
        }
        catch (DbEntityValidationException e)
        {
            // SaveChanges will throw an exception

            var processor = new DbEntityValidationExceptionProcessor();
            var evt = new SentryEvent();
            processor.Process(e, evt);

            Assert.True(evt.Data.TryGetValue("EntityValidationErrors", out var errors));
            var entityValidationErrors = errors as Dictionary<string, List<string>>;
            Assert.NotNull(entityValidationErrors);
            Assert.NotEmpty(entityValidationErrors);
        }
    }

    /// <summary>
    /// Ensure that the processor is also called and operated successfully inside an actual Sentry Client
    /// This should help avoid regression in case the underlying API changes in an unusual way
    /// </summary>
    [Fact]
    public async Task Integration_DbEntityValidationExceptionProcessorAsync()
    {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var context = new TestDbContext(connection, true);
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
        };
        options.AddEntityFramework();
        using var client = new SentryClient(options);
        // We use an actual Entity Framework instance since manually generating any EF related data is highly inaccurate
        context.TestTable.Add(new TestDbContext.TestData());
        try
        {
            // This will throw a validation exception since TestData has a Required column which we didn't set
            await context.SaveChangesAsync();
        }
        catch (DbEntityValidationException e)
        {
            Exception assertError = null;
            // SaveChanges will throw an exception
            options.SetBeforeSend((evt, _) =>
                {
                    // We use a try-catch here as we cannot assert directly since SentryClient itself would catch the thrown assertion errors
                    try
                    {
                        Assert.True(evt.Data.TryGetValue("EntityValidationErrors", out var errors));
                        var entityValidationErrors = errors as Dictionary<string, List<string>>;
                        Assert.NotNull(entityValidationErrors);
                        Assert.NotEmpty(entityValidationErrors);
                    }
                    catch (Exception ex)
                    {
                        assertError = ex;
                    }

                    return null;
                }
            );
            client.CaptureException(e);
            Assert.Null(assertError);
        }
    }
}
