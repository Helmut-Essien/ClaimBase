using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace ClaimBase.Api.Logging;

/// <summary>
/// Replaces secret property values before they reach a Serilog sink.
/// </summary>
public sealed class SecretRedactingPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SecretNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "JwtKey",
        "Key",
        "Token",
        "AccessToken"
    };

    /// <summary>
    /// Redacts properties whose names match known secrets.
    /// </summary>
    /// <param name="value">The value being logged.</param>
    /// <param name="propertyValueFactory">Factory for nested values.</param>
    /// <param name="result">The redacted structure when this policy handles <paramref name="value"/>.</param>
    /// <returns><see langword="true"/> when the value was rewritten.</returns>
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        ArgumentNullException.ThrowIfNull(propertyValueFactory);

        if (value is null || value is string)
        {
            result = null!;
            return false;
        }

        var properties = value.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .Select(property =>
            {
                var raw = property.GetValue(value);
                var logged = SecretNames.Contains(property.Name)
                    ? new ScalarValue("***")
                    : propertyValueFactory.CreatePropertyValue(raw, destructureObjects: true);
                return new LogEventProperty(property.Name, logged);
            })
            .ToList();

        if (properties.Count == 0)
        {
            result = null!;
            return false;
        }

        result = new StructureValue(properties);
        return true;
    }
}
