using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal;

internal class InstallationIdHelper(SentryOptions options)
{
    private readonly object _installationIdLock = new();
    private string? _installationId;

    public string? TryGetInstallationId()
    {
        // Installation ID could have already been resolved by this point
        if (!string.IsNullOrWhiteSpace(_installationId))
        {
            return _installationId;
        }

        // Resolve installation ID in a locked manner to guarantee consistency because ID can be non-deterministic.
        // Note: in the future, this probably has to be synchronized across multiple processes too.
        lock (_installationIdLock)
        {
            // We may have acquired the lock after another thread has already resolved
            // installation ID, so check the cache one more time before proceeding with I/O.
            if (!string.IsNullOrWhiteSpace(_installationId))
            {
                return _installationId;
            }

            var id =
                TryGetPersistentInstallationId() ??
                TryGetHardwareInstallationId() ??
                GetMachineNameInstallationId();

            if (!string.IsNullOrWhiteSpace(id))
            {
                options.LogDebug("Resolved installation ID '{0}'.", id);
            }
            else
            {
                options.LogDebug("Failed to resolve installation ID.");
            }

            return _installationId = id;
        }
    }

    private string? TryGetPersistentInstallationId()
    {
        if (options.DisableFileWrite)
        {
            options.LogDebug("File write has been disabled via the options. Skipping trying to get persistent installation ID.");
            return null;
        }

        try
        {
            var rootPath = options.CacheDirectoryPath ??
                           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directoryPath = Path.Combine(rootPath, "Sentry", options.Dsn!.GetHashString());
            var fileSystem = options.FileSystem;

            if (fileSystem.CreateDirectory(directoryPath) is not true)
            {
                options.LogDebug("Failed to create a directory for installation ID file ({0}).", directoryPath);
                return null;
            }

            options.LogDebug("Created directory for installation ID file ({0}).", directoryPath);

            var filePath = Path.Combine(directoryPath, ".installation");

            // Read installation ID stored in a file
            if (fileSystem.FileExists(filePath))
            {
                return fileSystem.ReadAllTextFromFile(filePath);
            }
            options.LogDebug("File containing installation ID does not exist ({0}).", filePath);

            // Generate new installation ID and store it in a file
            var id = Guid.NewGuid().ToString();
            if (fileSystem.WriteAllTextToFile(filePath, id) is not true)
            {
                options.LogDebug("Failed to write Installation ID to file ({0}).", filePath);
                return null;
            }

            options.LogDebug("Saved installation ID '{0}' to file '{1}'.", id, filePath);
            return id;
        }
        // If there's no write permission or the platform doesn't support this, we handle
        // and let the next installation id strategy kick in
        catch (Exception ex)
        {
            options.LogError(ex, "Failed to resolve persistent installation ID.");
            return null;
        }
    }

    private string? TryGetHardwareInstallationId()
    {
        try
        {
            // Get MAC address of the first network adapter
            var installationId = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic =>
                    nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(installationId))
            {
                options.LogError("Failed to find an appropriate network interface for installation ID.");
                return null;
            }

            return installationId;
        }
        catch (Exception ex)
        {
            options.LogError(ex, "Failed to resolve hardware installation ID.");
            return null;
        }
    }

    // Internal for testing
    internal static string GetMachineNameInstallationId() =>
        // Never fails
        Environment.MachineName.GetHashString();
}
