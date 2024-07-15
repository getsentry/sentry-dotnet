using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Describes the device that caused the event. This is most appropriate for mobile applications.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/"/>
public sealed class Device : ISentryJsonSerializable, ICloneable<Device>, IUpdatable<Device>
{
    /// <summary>
    /// Tells Sentry which type of context this is.
    /// </summary>
    public const string Type = "device";

    // TODO: remove this and replace with separate properties for 'timezone' and 'timezone_display_name'
    // since we don't carry enough data to deterministically recreate a TimeZoneInfo instance.
    /// <summary>
    /// The timezone of the device.
    /// </summary>
    /// <example>
    /// Europe/Vienna
    /// </example>
    public TimeZoneInfo? Timezone { get; set; }

    /// <summary>
    /// The name of the device. This is typically a hostname.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The manufacturer of the device.
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// The brand of the device.
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// The family of the device.
    /// </summary>
    /// <remarks>
    /// This is normally the common part of model names across generations.
    /// </remarks>
    /// <example>
    /// iPhone, Samsung Galaxy
    /// </example>
    public string? Family { get; set; }

    /// <summary>
    /// The model name.
    /// </summary>
    /// <example>
    /// Samsung Galaxy S3
    /// </example>
    public string? Model { get; set; }

    /// <summary>
    /// An internal hardware revision to identify the device exactly.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// The CPU architecture.
    /// </summary>
    public string? Architecture { get; set; }

    /// <summary>
    /// If the device has a battery an integer defining the battery level (in the range 0-100).
    /// </summary>
    public short? BatteryLevel { get; set; }

    /// <summary>
    /// True if the device is charging.
    /// </summary>
    public bool? IsCharging { get; set; }

    /// <summary>
    /// True if the device has a internet connection.
    /// </summary>
    public bool? IsOnline { get; set; }

    /// <summary>
    /// This can be a string portrait or landscape to define the orientation of a device.
    /// </summary>
    public DeviceOrientation? Orientation { get; set; }

    /// <summary>
    /// A boolean defining whether this device is a simulator or an actual device.
    /// </summary>
    public bool? Simulator { get; set; }

    /// <summary>
    /// Total system memory available in bytes.
    /// </summary>
    public long? MemorySize { get; set; }

    /// <summary>
    /// Free system memory in bytes.
    /// </summary>
    public long? FreeMemory { get; set; }

    /// <summary>
    /// Memory usable for the app in bytes.
    /// </summary>
    public long? UsableMemory { get; set; }

    /// <summary>
    /// True, if the device memory is low.
    /// </summary>
    public bool? LowMemory { get; set; }

    /// <summary>
    /// Total device storage in bytes.
    /// </summary>
    public long? StorageSize { get; set; }

    /// <summary>
    /// Free device storage in bytes.
    /// </summary>
    public long? FreeStorage { get; set; }

    /// <summary>
    /// Total size of an attached external storage in bytes (e.g.: android SDK card).
    /// </summary>
    public long? ExternalStorageSize { get; set; }

    /// <summary>
    /// Free size of an attached external storage in bytes (e.g.: android SDK card).
    /// </summary>
    public long? ExternalFreeStorage { get; set; }

    /// <summary>
    /// The resolution of the screen.
    /// </summary>
    /// <example>
    /// 800x600
    /// </example>
    public string? ScreenResolution { get; set; }

    /// <summary>
    /// The logical density of the display.
    /// </summary>
    public float? ScreenDensity { get; set; }

    /// <summary>
    /// The screen density as dots-per-inch.
    /// </summary>
    public int? ScreenDpi { get; set; }

    /// <summary>
    /// A formatted UTC timestamp when the system was booted.
    /// </summary>
    /// <example>
    /// 2018-02-08T12:52:12Z
    /// </example>
    public DateTimeOffset? BootTime { get; set; }

    /// <summary>
    /// Number of "logical processors".
    /// </summary>
    /// <example>
    /// 8
    /// </example>
    public int? ProcessorCount { get; set; }

    /// <summary>
    /// CPU description.
    /// </summary>
    /// <example>
    /// Intel(R) Core(TM)2 Quad CPU Q6600 @ 2.40GHz
    /// </example>
    public string? CpuDescription { get; set; }

    /// <summary>
    /// Processor frequency in MHz. Note that the actual CPU frequency might vary depending on current load and power
    /// conditions, especially on low-powered devices like phones and laptops. On some platforms it's not possible
    /// to query the CPU frequency. Currently such platforms are iOS and WebGL.
    /// </summary>
    /// <example>
    /// 2500
    /// </example>
    public int? ProcessorFrequency { get; set; }

    /// <summary>
    /// Kind of device the application is running on.
    /// </summary>
    /// <example>
    /// Unknown, Handheld, Console, Desktop
    /// </example>
    public string? DeviceType { get; set; }

    /// <summary>
    /// Status of the device's battery.
    /// </summary>
    /// <example>
    /// Unknown, Charging, Discharging, NotCharging, Full
    /// </example>
    public string? BatteryStatus { get; set; }

    /// <summary>
    /// Unique device identifier. Depends on the running platform.
    /// </summary>
    /// <example>
    /// iOS: UIDevice.identifierForVendor (UUID)
    /// Android: The generated Installation ID
    /// Windows Store Apps: AdvertisingManager::AdvertisingId (possible fallback to HardwareIdentification::GetPackageSpecificToken().Id)
    /// Windows Standalone: hash from the concatenation of strings taken from Computer System Hardware Classes
    /// </example>
    /// TODO: Investigate - Do ALL platforms now return a generated installation ID?
    ///       See https://github.com/getsentry/sentry-java/pull/1455
    public string? DeviceUniqueIdentifier { get; set; }

    /// <summary>
    /// Is vibration available on the device?
    /// </summary>
    public bool? SupportsVibration { get; set; }

    /// <summary>
    /// Is accelerometer available on the device?
    /// </summary>
    public bool? SupportsAccelerometer { get; set; }

    /// <summary>
    /// Is gyroscope available on the device?
    /// </summary>
    public bool? SupportsGyroscope { get; set; }

    /// <summary>
    /// Is audio available on the device?
    /// </summary>
    public bool? SupportsAudio { get; set; }

    /// <summary>
    /// Is the device capable of reporting its location?
    /// </summary>
    public bool? SupportsLocationService { get; set; }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    internal Device Clone() => ((ICloneable<Device>)this).Clone();

    Device ICloneable<Device>.Clone()
        => new()
        {
            Name = Name,
            Manufacturer = Manufacturer,
            Brand = Brand,
            Architecture = Architecture,
            BatteryLevel = BatteryLevel,
            IsCharging = IsCharging,
            IsOnline = IsOnline,
            BootTime = BootTime,
            ExternalFreeStorage = ExternalFreeStorage,
            ExternalStorageSize = ExternalStorageSize,
            ScreenResolution = ScreenResolution,
            ScreenDensity = ScreenDensity,
            ScreenDpi = ScreenDpi,
            Family = Family,
            FreeMemory = FreeMemory,
            FreeStorage = FreeStorage,
            MemorySize = MemorySize,
            Model = Model,
            ModelId = ModelId,
            Orientation = Orientation,
            Simulator = Simulator,
            StorageSize = StorageSize,
            Timezone = Timezone,
            UsableMemory = UsableMemory,
            LowMemory = LowMemory,
            ProcessorCount = ProcessorCount,
            CpuDescription = CpuDescription,
            ProcessorFrequency = ProcessorFrequency,
            SupportsVibration = SupportsVibration,
            DeviceType = DeviceType,
            BatteryStatus = BatteryStatus,
            DeviceUniqueIdentifier = DeviceUniqueIdentifier,
            SupportsAccelerometer = SupportsAccelerometer,
            SupportsGyroscope = SupportsGyroscope,
            SupportsAudio = SupportsAudio,
            SupportsLocationService = SupportsLocationService
        };

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    internal void UpdateFrom(Device source) => ((IUpdatable<Device>)this).UpdateFrom(source);

    void IUpdatable.UpdateFrom(object source)
    {
        if (source is Device device)
        {
            ((IUpdatable<Device>)this).UpdateFrom(device);
        }
    }

    void IUpdatable<Device>.UpdateFrom(Device source)
    {
        Name ??= source.Name;
        Manufacturer ??= source.Manufacturer;
        Brand ??= source.Brand;
        Architecture ??= source.Architecture;
        BatteryLevel ??= source.BatteryLevel;
        IsCharging ??= source.IsCharging;
        IsOnline ??= source.IsOnline;
        BootTime ??= source.BootTime;
        ExternalFreeStorage ??= source.ExternalFreeStorage;
        ExternalStorageSize ??= source.ExternalStorageSize;
        ScreenResolution ??= source.ScreenResolution;
        ScreenDensity ??= source.ScreenDensity;
        ScreenDpi ??= source.ScreenDpi;
        Family ??= source.Family;
        FreeMemory ??= source.FreeMemory;
        FreeStorage ??= source.FreeStorage;
        MemorySize ??= source.MemorySize;
        Model ??= source.Model;
        ModelId ??= source.ModelId;
        Orientation ??= source.Orientation;
        Simulator ??= source.Simulator;
        StorageSize ??= source.StorageSize;
        Timezone ??= source.Timezone;
        UsableMemory ??= source.UsableMemory;
        LowMemory ??= source.LowMemory;
        ProcessorCount ??= source.ProcessorCount;
        CpuDescription ??= source.CpuDescription;
        ProcessorFrequency ??= source.ProcessorFrequency;
        SupportsVibration ??= source.SupportsVibration;
        DeviceType ??= source.DeviceType;
        BatteryStatus ??= source.BatteryStatus;
        DeviceUniqueIdentifier ??= source.DeviceUniqueIdentifier;
        SupportsAccelerometer ??= source.SupportsAccelerometer;
        SupportsGyroscope ??= source.SupportsGyroscope;
        SupportsAudio ??= source.SupportsAudio;
        SupportsLocationService ??= source.SupportsLocationService;
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? _)
    {
        writer.WriteStartObject();

        writer.WriteString("type", Type);
        writer.WriteStringIfNotWhiteSpace("timezone", Timezone?.Id);

        // Write display name, but only if it's different from the ID
        if (!string.Equals(Timezone?.Id, Timezone?.DisplayName, StringComparison.OrdinalIgnoreCase))
        {
            writer.WriteStringIfNotWhiteSpace("timezone_display_name", Timezone?.DisplayName);
        }

        writer.WriteStringIfNotWhiteSpace("name", Name);
        writer.WriteStringIfNotWhiteSpace("manufacturer", Manufacturer);
        writer.WriteStringIfNotWhiteSpace("brand", Brand);
        writer.WriteStringIfNotWhiteSpace("family", Family);
        writer.WriteStringIfNotWhiteSpace("model", Model);
        writer.WriteStringIfNotWhiteSpace("model_id", ModelId);
        writer.WriteStringIfNotWhiteSpace("arch", Architecture);
        writer.WriteNumberIfNotNull("battery_level", BatteryLevel);
        writer.WriteBooleanIfNotNull("charging", IsCharging);
        writer.WriteBooleanIfNotNull("online", IsOnline);
        writer.WriteStringIfNotWhiteSpace("orientation", Orientation?.ToString().ToLowerInvariant());
        writer.WriteBooleanIfNotNull("simulator", Simulator);
        writer.WriteNumberIfNotNull("memory_size", MemorySize);
        writer.WriteNumberIfNotNull("free_memory", FreeMemory);
        writer.WriteNumberIfNotNull("usable_memory", UsableMemory);
        writer.WriteBooleanIfNotNull("low_memory", LowMemory);
        writer.WriteNumberIfNotNull("storage_size", StorageSize);
        writer.WriteNumberIfNotNull("free_storage", FreeStorage);
        writer.WriteNumberIfNotNull("external_storage_size", ExternalStorageSize);
        writer.WriteNumberIfNotNull("external_free_storage", ExternalFreeStorage);
        writer.WriteStringIfNotWhiteSpace("screen_resolution", ScreenResolution);
        writer.WriteNumberIfNotNull("screen_density", ScreenDensity);
        writer.WriteNumberIfNotNull("screen_dpi", ScreenDpi);
        writer.WriteStringIfNotNull("boot_time", BootTime);
        writer.WriteNumberIfNotNull("processor_count", ProcessorCount);
        writer.WriteStringIfNotWhiteSpace("cpu_description", CpuDescription);
        writer.WriteNumberIfNotNull("processor_frequency", ProcessorFrequency);
        writer.WriteStringIfNotWhiteSpace("device_type", DeviceType);
        writer.WriteStringIfNotWhiteSpace("battery_status", BatteryStatus);
        writer.WriteStringIfNotWhiteSpace("device_unique_identifier", DeviceUniqueIdentifier);
        writer.WriteBooleanIfNotNull("supports_vibration", SupportsVibration);
        writer.WriteBooleanIfNotNull("supports_accelerometer", SupportsAccelerometer);
        writer.WriteBooleanIfNotNull("supports_gyroscope", SupportsGyroscope);
        writer.WriteBooleanIfNotNull("supports_audio", SupportsAudio);
        writer.WriteBooleanIfNotNull("supports_location_service", SupportsLocationService);

        writer.WriteEndObject();
    }

    private static TimeZoneInfo? TryParseTimezone(JsonElement json)
    {
        var timezoneId = json.GetPropertyOrNull("timezone")?.GetString();
        var timezoneName = json.GetPropertyOrNull("timezone_display_name")?.GetString() ?? timezoneId;

        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(timezoneId, TimeSpan.Zero, timezoneName, timezoneName);
        }
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static Device FromJson(JsonElement json)
    {
        var timezone = TryParseTimezone(json);
        var name = json.GetPropertyOrNull("name")?.GetString();
        var manufacturer = json.GetPropertyOrNull("manufacturer")?.GetString();
        var brand = json.GetPropertyOrNull("brand")?.GetString();
        var family = json.GetPropertyOrNull("family")?.GetString();
        var model = json.GetPropertyOrNull("model")?.GetString();
        var modelId = json.GetPropertyOrNull("model_id")?.GetString();
        var architecture = json.GetPropertyOrNull("arch")?.GetString();

        // TODO: For next mayor: Remove this and change batteryLevel from short to float
        // The Java and Cocoa SDK report the battery as `float`
        // Cocoa https://github.com/getsentry/sentry-cocoa/blob/e773cad622b86735f1673368414009475e4119fd/Sources/Sentry/include/SentryUIDeviceWrapper.h#L18
        // Java  https://github.com/getsentry/sentry-java/blob/25f1ca4e1636a801c17c1662f0145f888550bce8/sentry/src/main/java/io/sentry/protocol/Device.java#L231-L233
        short? batteryLevel = null;
        var batteryProperty = json.GetPropertyOrNull("battery_level");
        if (batteryProperty.HasValue)
        {
            batteryLevel = (short)batteryProperty.Value.GetDouble();
        }

        var isCharging = json.GetPropertyOrNull("charging")?.GetBoolean();
        var isOnline = json.GetPropertyOrNull("online")?.GetBoolean();
        var orientation = json.GetPropertyOrNull("orientation")?.GetString()?.ParseEnum<DeviceOrientation>();
        var simulator = json.GetPropertyOrNull("simulator")?.GetBoolean();
        var memorySize = json.GetPropertyOrNull("memory_size")?.GetInt64();
        var freeMemory = json.GetPropertyOrNull("free_memory")?.GetInt64();
        var usableMemory = json.GetPropertyOrNull("usable_memory")?.GetInt64();
        var lowMemory = json.GetPropertyOrNull("low_memory")?.GetBoolean();
        var storageSize = json.GetPropertyOrNull("storage_size")?.GetInt64();
        var freeStorage = json.GetPropertyOrNull("free_storage")?.GetInt64();
        var externalStorageSize = json.GetPropertyOrNull("external_storage_size")?.GetInt64();
        var externalFreeStorage = json.GetPropertyOrNull("external_free_storage")?.GetInt64();
        var screenResolution = json.GetPropertyOrNull("screen_resolution")?.GetString();
        var screenDensity = json.GetPropertyOrNull("screen_density")?.GetSingle();
        var screenDpi = json.GetPropertyOrNull("screen_dpi")?.GetInt32();
        var bootTime = json.GetPropertyOrNull("boot_time")?.GetDateTimeOffset();
        var processorCount = json.GetPropertyOrNull("processor_count")?.GetInt32();
        var cpuDescription = json.GetPropertyOrNull("cpu_description")?.GetString();
        var processorFrequency = json.GetPropertyOrNull("processor_frequency")?.GetInt32();
        var deviceType = json.GetPropertyOrNull("device_type")?.GetString();
        var batteryStatus = json.GetPropertyOrNull("battery_status")?.GetString();
        var deviceUniqueIdentifier = json.GetPropertyOrNull("device_unique_identifier")?.GetString();
        var supportsVibration = json.GetPropertyOrNull("supports_vibration")?.GetBoolean();
        var supportsAccelerometer = json.GetPropertyOrNull("supports_accelerometer")?.GetBoolean();
        var supportsGyroscope = json.GetPropertyOrNull("supports_gyroscope")?.GetBoolean();
        var supportsAudio = json.GetPropertyOrNull("supports_audio")?.GetBoolean();
        var supportsLocationService = json.GetPropertyOrNull("supports_location_service")?.GetBoolean();

        return new Device
        {
            Timezone = timezone,
            Name = name,
            Manufacturer = manufacturer,
            Brand = brand,
            Family = family,
            Model = model,
            ModelId = modelId,
            Architecture = architecture,
            BatteryLevel = batteryLevel,
            IsCharging = isCharging,
            IsOnline = isOnline,
            Orientation = orientation,
            Simulator = simulator,
            MemorySize = memorySize,
            FreeMemory = freeMemory,
            UsableMemory = usableMemory,
            LowMemory = lowMemory,
            StorageSize = storageSize,
            FreeStorage = freeStorage,
            ExternalStorageSize = externalStorageSize,
            ExternalFreeStorage = externalFreeStorage,
            ScreenResolution = screenResolution,
            ScreenDensity = screenDensity,
            ScreenDpi = screenDpi,
            BootTime = bootTime,
            ProcessorCount = processorCount,
            CpuDescription = cpuDescription,
            ProcessorFrequency = processorFrequency,
            DeviceType = deviceType,
            BatteryStatus = batteryStatus,
            DeviceUniqueIdentifier = deviceUniqueIdentifier,
            SupportsVibration = supportsVibration,
            SupportsAccelerometer = supportsAccelerometer,
            SupportsGyroscope = supportsGyroscope,
            SupportsAudio = supportsAudio,
            SupportsLocationService = supportsLocationService
        };
    }
}
