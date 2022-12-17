namespace Driver.Models.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ModelAttribute : Attribute
{
    public string? Name { get; set; }

    public ModelAttribute(string? name = null)
    {
        Name = name;
    }

}