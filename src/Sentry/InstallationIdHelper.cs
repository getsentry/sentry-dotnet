using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

internal class InstallationIdHelper
{
    private readonly object _installationIdLock = new();
    private string? _installationId;
    private readonly SentryOptions _options;
    private readonly string? _persistenceDirectoryPath;

    private Lazy<string?> LazyInstallationId => new(TryGetInstallationId);
    public string?InstallationId => LazyInstallationId.Value;

    public InstallationIdHelper(SentryOptions options, string? persistenceDirectoryPath = null)
    {
        _options = options;
        // TODO: session file should really be process-isolated, but we
        // don't have a proper mechanism for that right now.
        _persistenceDirectoryPath = persistenceDirectoryPath ?? options.TryGetDsnSpecificCacheDirectoryPath();
    }

    private string? TryGetInstallationId()
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
                _options.LogDebug("Resolved installation ID '{0}'.", id);
            }
            else
            {
                _options.LogDebug("Failed to resolve installation ID.");
            }

            return _installationId = id;
        }
    }

    private string? TryGetPersistentInstallationId()
    {
        try
        {
            var directoryPath =
                _persistenceDirectoryPath
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Sentry",
                    _options.Dsn!.GetHashString());

            Directory.CreateDirectory(directoryPath);

            _options.LogDebug("Created directory for installation ID file ({0}).", directoryPath);

            var filePath = Path.Combine(directoryPath, ".installation");

            // Read installation ID stored in a file
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            _options.LogDebug("File containing installation ID does not exist ({0}).", filePath);

            // Generate new installation ID and store it in a file
            var id = Guid.NewGuid().ToString();
            File.WriteAllText(filePath, id);

            _options.LogDebug("Saved installation ID '{0}' to file '{1}'.", id, filePath);
            return id;
        }
        // If there's no write permission or the platform doesn't support this, we handle
        // and let the next installation id strategy kick in
        catch (Exception ex)
        {
            _options.LogError(ex, "Failed to resolve persistent installation ID.");
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
                _options.LogError("Failed to find an appropriate network interface for installation ID.");
                return null;
            }

            return installationId;
        }
        catch (Exception ex)
        {
            _options.LogError(ex, "Failed to resolve hardware installation ID.");
            return null;
        }
    }

    // Internal for testing
    internal static string GetMachineNameInstallationId() =>
        // Never fails
        Environment.MachineName.GetHashString();
}
