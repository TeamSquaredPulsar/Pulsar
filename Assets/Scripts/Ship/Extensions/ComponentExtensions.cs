using System;
using System.Reflection;
using UnityEngine;

namespace Pulsar.Ship.Ship.Extensions
{
public static class ComponentExtensions
{
    private const BindingFlags AllFlags = BindingFlags.Public |
                                                    BindingFlags.Instance |
                                                    BindingFlags.NonPublic;
    public static T CopyComponentSettingsTo<T>(
        this T original,
        in T destination) where T : Component
    {

        Type type = original.GetType();

        // Copy Fields (variables)
        FieldInfo[] fields = type.GetFields(AllFlags);
        foreach (FieldInfo field in fields)
        {
            field.SetValue(destination, field.GetValue(original));
        }

        // Copy Properties (like colours, materials, enabled states)
        PropertyInfo[] properties = type.GetProperties(AllFlags);
        foreach (PropertyInfo property in properties)
        {
            // Only copy if the property can be read and written to, and isn't obsolete
            if (property.CanWrite && property.CanRead &&
                property.Name != "name")
            {
                try
                {
                    property.SetValue(
                        destination, 
                        property.GetValue(original, null),
                        null
                    );
                }
                catch
                {
                    // Catch cases where certain native unity properties reject value injection
                }
            }
        }

        return destination;
    }
}
}