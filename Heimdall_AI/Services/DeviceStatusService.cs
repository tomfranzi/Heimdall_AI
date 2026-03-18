using System.Text.Json;

namespace Heimdall_AI.Services;

public interface IDeviceStatusService
{
    DeviceStatusInfo? CurrentStatus { get; }
    event Action<DeviceStatusInfo>? StatusUpdated;
    void UpdateFromPayload(string payload);
}

public sealed class DeviceStatusService : IDeviceStatusService
{
    public DeviceStatusInfo? CurrentStatus { get; private set; }

    public event Action<DeviceStatusInfo>? StatusUpdated;

    public void UpdateFromPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var status = ReadString(root, "status") ?? "unknown";
            var uptime = ReadString(root, "uptime") ?? "--";
            var cpuTemp = ReadString(root, "cpu_temp") ?? "--.-°C";
            var deviceId = ReadString(root, "device_id") ?? "RPI_HEIMDALL";

            var info = new DeviceStatusInfo
            {
                Status = status,
                Uptime = uptime,
                CpuTemp = cpuTemp,
                DeviceId = deviceId
            };

            CurrentStatus = info;
            StatusUpdated?.Invoke(info);
        }
        catch
        {
        }
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return value.ToString();
    }
}

public sealed class DeviceStatusInfo
{
    public string Status { get; set; } = "unknown";
    public string Uptime { get; set; } = "--";
    public string CpuTemp { get; set; } = "--.-°C";
    public string DeviceId { get; set; } = "RPI_HEIMDALL";
}
