#nullable disable
using System.Reflection;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// Extension methods for <see cref="Enum"/>
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets all flags in the specified <see cref="Enum"/>
    /// </summary>
    /// <param name="input">The <see cref="Enum"/> to check</param>
    /// <returns>The flags</returns>
    /// <exception cref="ArgumentException">input is not an <see cref="Enum"/></exception>
    /// <exception cref="ArgumentNullException">input is null</exception>
    /// <exception cref="InvalidOperationException">The method is invoked by reflection in a reflection-only context, -or- 
    /// input s a type from an assembly loaded in a reflection-only context</exception>
    public static IEnumerable<Enum> GetFlags(this Enum input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        foreach (Enum value in Enum.GetValues(input.GetType()))
            if (input.HasFlag(value))
                yield return value;
    }

    /// <summary>
    /// Gets all flags in the specified <see cref="Enum"/>
    /// </summary>
    /// <typeparam name="T">The type of enum</typeparam>
    /// <param name="input">The <see cref="Enum"/> to check</param>
    /// <returns>The flags</returns>
    /// <exception cref="ArgumentException">input is not an <see cref="Enum"/></exception>
    /// <exception cref="ArgumentNullException">input is null</exception>
    /// <exception cref="InvalidOperationException">The method is invoked by reflection in a reflection-only context, -or- 
    /// input s a type from an assembly loaded in a reflection-only context</exception>
    public static IEnumerable<T> GetFlags<T>(this T input)
        where T : Enum
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        foreach (Enum value in Enum.GetValues(input.GetType()))
            if (input.HasFlag(value))
                yield return (T)value;
    }

    /// <summary>
    /// Sets the specified flag on the enum value
    /// </summary>
    /// <typeparam name="T">The enum type</typeparam>
    /// <param name="value">The enum value</param>
    /// <param name="flag">The flag to set</param>
    /// <param name="set">True to set the flag, false to clear it</param>
    /// <returns>The new enum value</returns>
    public static T SetFlag<T>(this Enum value, T flag, bool set)
        where T : Enum
    {
        Type underlyingType = Enum.GetUnderlyingType(value.GetType());

        dynamic valueAsInt = Convert.ChangeType(value, underlyingType);
        dynamic flagAsInt = Convert.ChangeType(flag, underlyingType);

        if (set)
            valueAsInt |= flagAsInt;
        else
            valueAsInt &= ~flagAsInt;

        return (T)valueAsInt;
    }

    /// <summary>
    /// Gets all values for an <see cref="Enum"/>
    /// </summary>
    /// <typeparam name="T">The type of enum</typeparam>
    /// <param name="input">The enum to get values from</param>
    /// <returns>An array with the values</returns>
    /// <exception cref="ArgumentException">input is not an <see cref="Enum"/></exception>
    /// <exception cref="ArgumentNullException">input is null</exception>
    /// <exception cref="InvalidOperationException">The method is invoked by reflection in a reflection-only context, -or- 
    /// input s a type from an assembly loaded in a reflection-only context</exception>
    public static T[] GetValues<T>(this T input)
        where T : Enum
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        return EnumHelpers.GetValues<T>();
    }

#nullable enable

    /// <summary>
    /// Gets the first attribute of specified type for the enum value
    /// </summary>
    /// <typeparam name="T">The type of attribute to retrieve</typeparam>
    /// <param name="value">The enum value to get the attribute for</param>
    /// <returns>The attribute instance</returns>
    public static T? GetAttribute<T>(this Enum value) 
        where T : Attribute
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        // Get the member info for the value
        MemberInfo[] memberInfo = value.GetType().GetMember(value.ToString());

        // Get the attribute
        IEnumerable<T> attributes = memberInfo.First().GetCustomAttributes<T>(false);

        // Return the first attribute
        return attributes.FirstOrDefault();
    }

    public static T GetRequiredAttribute<T>(this Enum value)
        where T : Attribute
    {
        return value.GetAttribute<T>() ?? throw new Exception($"The attribute of type {typeof(T)} was not found on the value {value}");
    }
}