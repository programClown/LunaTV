using System;
using System.Globalization;
using Avalonia.Controls;

namespace LunaTV.Extensions;

internal static class Extensions
{
    /// <summary>
    /// Returns the template element with specified name and type, or throws an exception if not found. 
    /// </summary>
    /// <param name="nameScope">The NameScope used to find the element.</param>
    /// <param name="name">The name of the element to find.</param>
    /// <typeparam name="T">The type of the element to find.</typeparam>
    /// <returns>The element found.</returns>
    /// <exception cref="InvalidCastException">The element is not found.</exception>
    public static  T FindOrThrow<T>(this INameScope nameScope, string name)
        where T : class
    {
        var part = nameScope.Find<T>(name);
        if (part == null)
        {
            throw new InvalidCastException($"Could not find template element '{name}' of type '{typeof(T).Name}'.");
        }
        return part;
    }
}