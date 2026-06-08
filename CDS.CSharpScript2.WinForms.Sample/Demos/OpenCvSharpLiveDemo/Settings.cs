using Newtonsoft.Json;

namespace CDS.CSharpScript2.WinForms.Sample.Demos.OpenCvSharpLiveDemo;

/// <summary>
/// Persisted settings for the OpenCvSharp live demo.
/// </summary>
public class Settings
{
    /// <summary>Gets or sets the script executed on each captured frame.</summary>
    [JsonProperty(PropertyName = "Script_V1")]
    public string Script { get; set; } = "Cv2.Canny(Source, Dest, 100, 200);";
}
