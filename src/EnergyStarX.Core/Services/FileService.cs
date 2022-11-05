using System.Text;

using EnergyStarX.Core.Contracts.Services;

using Newtonsoft.Json;

namespace EnergyStarX.Core.Services;

public class FileService : IFileService
{
    private static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    public T Read<T>(string folderPath, string fileName)
    {
        semaphoreSlim.Wait();
        try
        {
            var path = Path.Combine(folderPath, fileName);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<T>(json);
            }
            return default;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        semaphoreSlim.Wait();
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            var fileContent = JsonConvert.SerializeObject(content);
            File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    public void Delete(string folderPath, string fileName)
    {
        semaphoreSlim.Wait();
        try
        {
            if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
            {
                File.Delete(Path.Combine(folderPath, fileName));
            }
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }
}
