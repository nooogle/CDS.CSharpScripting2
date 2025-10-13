namespace CDS.CSharpScript2;


/// <summary>
/// Category attribute for design-time properties and events in the "CDS" category.
/// </summary>
public class CDSCategoryAttribute : System.ComponentModel.CategoryAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CDSCategoryAttribute"/> class with the category name "CDS".
    /// </summary>
    public CDSCategoryAttribute() : base("CDS")
    {
    }
}
