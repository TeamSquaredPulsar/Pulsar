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

    public static Rigidbody2D SetBuildRb(
        this Rigidbody2D original
    )
    {
        original.linearVelocity = Vector2.zero;
        original.angularVelocity = 0f;
        original.bodyType = RigidbodyType2D.Kinematic;
        return original;
    }

    public static Rigidbody2D SetFloatRb(
        this Rigidbody2D original,
        Vector2 velocity,
        float angularVelocity
    )
    {
        original.bodyType = RigidbodyType2D.Dynamic;
        original.constraints = RigidbodyConstraints2D.None;
        original.gravityScale = 0f;
        original.linearDamping = 0.05f;
        original.angularDamping = 0.1f;
        original.linearVelocity = velocity;
        original.angularVelocity = angularVelocity;
        return original;
    }
}
}