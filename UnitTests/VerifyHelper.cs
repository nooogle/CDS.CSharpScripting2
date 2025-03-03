[assembly: UsesVerify]


namespace VerifyMSTestHelpers;


/// <summary>
/// Custom JSON converter for double values that provides special handling for
/// NaN, Infinity, and ensures consistent string formatting.
/// </summary>
internal class CustomDoubleConverter : Argon.JsonConverter<double>
{
    /// <summary>
    /// Reads the JSON representation of a double value.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="type">The object type.</param>
    /// <param name="existingValue">The existing value of object being read.</param>
    /// <param name="hasExisting">True if there's an existing value.</param>
    /// <param name="serializer">The JSON serializer.</param>
    /// <returns>The deserialized double value.</returns>
    public override double ReadJson(Argon.JsonReader reader, Type type, double existingValue, bool hasExisting, Argon.JsonSerializer serializer)
    {
        // Convert the read value to double, ensuring it's not null
        return (double)reader.ReadAsDouble()!.Value;
    }

    /// <summary>
    /// Writes the JSON representation of a double value with special handling for NaN and Infinity values,
    /// and consistent string formatting for regular values.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The double value to write.</param>
    /// <param name="serializer">The JSON serializer.</param>
    public override void WriteJson(Argon.JsonWriter writer, double value, Argon.JsonSerializer serializer)
    {
        // Handle special cases
        if (double.IsNaN(value))
        {
            writer.WriteValue("NaN");
            return;
        }

        if (double.IsInfinity(value))
        {
            // Write negative or positive infinity as appropriate
            writer.WriteValue(value < 0 ? "-Infinity" : "Infinity");
            return;
        }

        // Format regular double values with 17 significant digits (G17) for maximum precision
        // and use the invariant culture to ensure consistency across different environments
        string formattedValue = value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);

        writer.WriteValue(formattedValue);
    }
}


/// <summary>
/// Custom JSON converter for float values that provides special handling for
/// NaN, Infinity, and ensures consistent string formatting.
/// </summary>
internal class CustomFloatConverter : Argon.JsonConverter<float>
{
    /// <summary>
    /// Reads the JSON representation of a float value.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="type">The object type.</param>
    /// <param name="existingValue">The existing value of object being read.</param>
    /// <param name="hasExisting">True if there's an existing value.</param>
    /// <param name="serializer">The JSON serializer.</param>
    /// <returns>The deserialized float value.</returns>
    public override float ReadJson(Argon.JsonReader reader, Type type, float existingValue, bool hasExisting, Argon.JsonSerializer serializer)
    {
        // Convert the read value to float from double, ensuring it's not null
        return (float)reader.ReadAsDouble()!.Value;
    }

    /// <summary>
    /// Writes the JSON representation of a float value with special handling for NaN and Infinity values,
    /// and consistent string formatting for regular values.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The float value to write.</param>
    /// <param name="serializer">The JSON serializer.</param>
    public override void WriteJson(Argon.JsonWriter writer, float value, Argon.JsonSerializer serializer)
    {
        // Handle special cases
        if (float.IsNaN(value))
        {
            writer.WriteValue("NaN");
            return;
        }

        if (float.IsInfinity(value))
        {
            // Write negative or positive infinity as appropriate
            writer.WriteValue(value < 0 ? "-Infinity" : "Infinity");
            return;
        }

        // Format regular float values with 9 significant digits (G9) for appropriate precision
        // This is sufficient for single-precision floating point representation
        // and use the invariant culture to ensure consistency across different environments
        string formattedValue = value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture);

        writer.WriteValue(formattedValue);
    }
}


/// <summary>
/// Performs checks on the Verify settings to ensure they are correctly configured.
/// </summary>
[TestClass]
public partial class VerifyCheck
{
    /// <summary>
    /// Runs the Verify checks which can help with customization and configuration of the git 
    /// settings that help manage the verification files.
    /// </summary>
    [TestMethod]
    public async Task Run()
    {
        await VerifyChecks.Run();
    }
}


/// <summary>
/// Helper class to customise the handling for floating-point values when verifying test results.
/// This runs during the assembly initialization to ensure that the custom settings are applied
/// once for all tests in the assembly.
/// </summary>
[TestClass()]
public sealed partial class VerifyHelper
{
    /// <summary>
    /// Initializes the default settings by customising the JSON settings
    /// for floating-point values. This ensures that the JSON representation
    /// for floating-point values is consistent across different environments
    /// such as .Net Framework and .Net 8.
    /// </summary>
    [AssemblyInitialize()]
    public static void AssemblyInit(TestContext context)
    {
        // By default, empty or 0 values are excluded from the verification results.
        // This change ensures that all data is included.
        VerifierSettings.AddExtraSettings(serializerSettings =>
        {
            serializerSettings.DefaultValueHandling = Argon.DefaultValueHandling.Include;
        });


        // Add custom converters for float and double values to ensure consistent
        // string formatting and handling of special cases such as NaN and Infinity.
        VerifierSettings.AddExtraSettings(serializer =>
        {
            serializer.Converters.Add(new CustomFloatConverter());
            serializer.Converters.Add(new CustomDoubleConverter());
        });
    }
}
