// This can be used to test the functionality of Sentry functionality in a single-file app.
//
// The Directory.Build.props file for this project sets DefaultTargets="Build;Publish", so this project automatically
// gets published whenever it's built using the dotnet build or dotnet test commands. A RuntimeIdentifier is required
// for publishing, which gets set in the csproj file.
Console.WriteLine("Hello, World!");
