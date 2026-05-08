using System.Text.Json;

namespace TestUtils;

/// <summary>
/// Utility to provide file load/save for a JSON-compatible data type.
/// </summary>
/// <typeparam name="T">Data type to load and save</typeparam>
public class SettingsManager<T> where T : new()
{
    /// <summary>
    /// Filename of the JSON file
    /// </summary>
    private readonly string fileName;

    /// <summary>The loaded (or created) instance</summary>
    public T Settings { get; private set; }

    /// <summary>
    /// Loads an existing JSON file or creates a new one, and deserialises the contents into the <see cref="Settings"/> property.
    /// </summary>
    /// <param name="name">The name part of the JSON filename</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null</exception>
    /// <exception cref="IOException">Thrown when file operations fail</exception>
    public SettingsManager(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name), "Settings name cannot be null or empty");
        }

        Settings = new T();

        try
        {
            var applicationFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Application.ProductName ?? "DefaultApp");

            Directory.CreateDirectory(applicationFolder);

            fileName = Path.Combine(applicationFolder, $"{name}.json");

            if (File.Exists(fileName))
            {
                string jsonContentFromFile = File.ReadAllText(fileName);
                if (!string.IsNullOrEmpty(jsonContentFromFile))
                {
                    var deserializedSettings = JsonSerializer.Deserialize<T>(jsonContentFromFile);
                    if (deserializedSettings != null)
                    {
                        Settings = deserializedSettings;
                    }
                }
            }

            Save();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException || ex is JsonException)
        {
            throw new IOException($"Failed to initialize settings for '{name}'", ex);
        }
    }

    /// <summary>
    /// Serialises <see cref="Settings"/> and saves to disk. A backup is
    /// made if the data has changed.
    /// </summary>
    /// <exception cref="IOException">Thrown when file operations fail</exception>
    public void Save()
    {
        if (string.IsNullOrEmpty(fileName))
        {
            throw new InvalidOperationException("Filename was not properly initialized");
        }

        try
        {
            // Create a backup if file exists
            if (File.Exists(fileName))
            {
                string backupFile = $"{fileName}.bak";
                File.Copy(fileName, backupFile, overwrite: true);
            }

            string dataAsJSON = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fileName, dataAsJSON);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
        {
            throw new IOException($"Failed to save settings to '{fileName}'", ex);
        }
    }
}
