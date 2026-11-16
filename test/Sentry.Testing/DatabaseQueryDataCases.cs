namespace Sentry.Testing;

public static class DatabaseQueryDataCases
{
    public static TheoryData<bool, bool?, bool> SendDefaultPiiAndConfiguredValue_Collected => new()
    {
        { false, null, true },
        { true, null, true },
        { false, true, true },
        { true, false, false },
    };

    public static SentryOptions WithDatabaseQueryData(this SentryOptions options, bool sendDefaultPii, bool? databaseQueryData)
    {
        options.SendDefaultPii = sendDefaultPii;
        if (databaseQueryData is { } collect)
        {
            options.ConfigureDataCollection(dataCollection => dataCollection.DatabaseQueryData = collect);
        }

        return options;
    }
}
