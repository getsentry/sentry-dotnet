using System;
using System.Text.Json;
using Sentry.Internal.Extensions;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Describes the device that caused the event. This is most appropriate for mobile applications.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/"/>
    public sealed class Device : IJsonSerializable
    {
        /// <summary>
        /// Tells Sentry which type of context this is.
        /// </summary>
        public const string Type = "device";

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
        /// 018-02-08T12:52:12Z
        /// </example>
        public DateTimeOffset? BootTime { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        internal Device Clone()
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
                LowMemory = LowMemory
            };

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteString("type", Type);

            if (Timezone is {} timezone)
            {
                writer.WriteString("timezone", timezone.Id);
                writer.WriteString("timezone_display_name", timezone.DisplayName);
            }

            if (!string.IsNullOrWhiteSpace(Name))
            {
                writer.WriteString("name", Name);
            }

            if (!string.IsNullOrWhiteSpace(Manufacturer))
            {
                writer.WriteString("manufacturer", Manufacturer);
            }

            if (!string.IsNullOrWhiteSpace(Brand))
            {
                writer.WriteString("brand", Brand);
            }

            if (!string.IsNullOrWhiteSpace(Family))
            {
                writer.WriteString("family", Family);
            }

            if (!string.IsNullOrWhiteSpace(Model))
            {
                writer.WriteString("model", Model);
            }

            if (!string.IsNullOrWhiteSpace(ModelId))
            {
                writer.WriteString("model_id", ModelId);
            }

            if (!string.IsNullOrWhiteSpace(Architecture))
            {
                writer.WriteString("arch", Architecture);
            }

            if (BatteryLevel is {} batteryLevel)
            {
                writer.WriteNumber("battery_level", batteryLevel);
            }

            if (IsCharging is {} isCharging)
            {
                writer.WriteBoolean("charging", isCharging);
            }

            if (IsOnline is {} isOnline)
            {
                writer.WriteBoolean("online", isOnline);
            }

            if (Orientation is {} orientation)
            {
                writer.WriteString("orientation", orientation.ToString().ToLowerInvariant());
            }

            if (Simulator is {} simulator)
            {
                writer.WriteBoolean("simulator", simulator);
            }

            if (MemorySize is {} memorySize)
            {
                writer.WriteNumber("memory_size", memorySize);
            }

            if (FreeMemory is {} freeMemory)
            {
                writer.WriteNumber("free_memory", freeMemory);
            }

            if (UsableMemory is {} usableMemory)
            {
                writer.WriteNumber("usable_memory", usableMemory);
            }

            if (LowMemory is {} lowMemory)
            {
                writer.WriteBoolean("low_memory", lowMemory);
            }

            if (StorageSize is {} storageSize)
            {
                writer.WriteNumber("storage_size", storageSize);
            }

            if (FreeStorage is {} freeStorage)
            {
                writer.WriteNumber("free_storage", freeStorage);
            }

            if (ExternalStorageSize is {} externalStorageSize)
            {
                writer.WriteNumber("external_storage_size", externalStorageSize);
            }

            if (ExternalFreeStorage is {} externalFreeStorage)
            {
                writer.WriteNumber("external_free_storage", externalFreeStorage);
            }

            if (!string.IsNullOrWhiteSpace(ScreenResolution))
            {
                writer.WriteString("screen_resolution", ScreenResolution);
            }

            if (ScreenDensity is {} screenDensity)
            {
                writer.WriteNumber("screen_density", screenDensity);
            }

            if (ScreenDpi is {} screenDpi)
            {
                writer.WriteNumber("screen_dpi", screenDpi);
            }

            if (BootTime is {} bootTime)
            {
                writer.WriteString("boot_time", bootTime);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static Device FromJson(JsonElement json)
        {
            var timezone = json.GetPropertyOrNull("timezone")?.GetString()?.Pipe(TimeZoneInfo.FindSystemTimeZoneById);
            var name = json.GetPropertyOrNull("name")?.GetString();
            var manufacturer = json.GetPropertyOrNull("manufacturer")?.GetString();
            var brand = json.GetPropertyOrNull("brand")?.GetString();
            var family = json.GetPropertyOrNull("family")?.GetString();
            var model = json.GetPropertyOrNull("model")?.GetString();
            var modelId = json.GetPropertyOrNull("model_id")?.GetString();
            var architecture = json.GetPropertyOrNull("arch")?.GetString();
            var batteryLevel = json.GetPropertyOrNull("battery_level")?.GetInt16();
            var isCharging = json.GetPropertyOrNull("charging")?.GetBoolean();
            var isOnline = json.GetPropertyOrNull("online")?.GetBoolean();
            var orientation = json.GetPropertyOrNull("orientation")?.GetString()?.Pipe(s => s.ParseEnum<DeviceOrientation>());
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
                BootTime = bootTime
            };
        }
    }
}
