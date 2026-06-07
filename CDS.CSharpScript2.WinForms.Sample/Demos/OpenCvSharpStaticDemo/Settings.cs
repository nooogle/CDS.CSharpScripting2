using Newtonsoft.Json;

namespace CDS.CSharpScript2.WinForms.Sample.Demos.OpenCvSharpStaticDemo;

/// <summary>
/// Settings for the demo
/// </summary>
public class Settings
{
    /// <summary>
    /// The script to run
    /// </summary>
    [JsonProperty(PropertyName = "Script_V2")]
    public string Script { get; set; } = "Cv2.Canny(Source, Dest, 100, 200);";
}
