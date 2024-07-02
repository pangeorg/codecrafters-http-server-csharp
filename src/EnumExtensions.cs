using System.ComponentModel;
using System.Reflection;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        return value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DescriptionAttribute>() is { } attribute
            ? attribute.Description
            : value.ToString();
    }
}

