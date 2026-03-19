using System.Text.Json;

namespace Heimdall_AI.Services;

public interface IFileDatabaseService
{
    Task<T> LoadAsync<T>(string fileName, T fallback, CancellationToken cancellationToken = default);
    Task SaveAsync<T>(string fileName, T data, CancellationToken cancellationToken = default);
}

public sealed class FileDatabaseService : IFileDatabaseService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _ioLock = new(1, 1);

    public async Task<T> LoadAsync<T>(string fileName, T fallback, CancellationToken cancellationToken = default)
    {
        var path = GetPath(fileName);

        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(path))
            {
                return fallback;
            }

            await using var stream = File.OpenRead(path);
            var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
            return data is null ? fallback : data;
        }
        catch
        {
            return fallback;
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async Task SaveAsync<T>(string fileName, T data, CancellationToken cancellationToken = default)
    {
        var path = GetPath(fileName);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, data, JsonOptions, cancellationToken);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private static string GetPath(string fileName)
    {
        return Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
    }
}
