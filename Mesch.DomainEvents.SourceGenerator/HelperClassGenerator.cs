using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mesch.DomainEvents;

/// <summary>
/// Generates helper classes with static methods for creating domain events.
/// </summary>
internal static class HelperClassGenerator
{
    /// <summary>
    /// Generates a helper class with static methods for creating events.
    /// </summary>
    /// <param name="methods">The methods to generate helper methods for.</param>
    /// <param name="config">The generation configuration.</param>
    /// <param name="classSymbol">The containing class symbol.</param>
    /// <returns>The generated helper class code.</returns>
    public static string GenerateHelperClass(List<IMethodSymbol> methods, EventGenerationConfig config, INamedTypeSymbol classSymbol)
    {
        var className = config.HelperClassName ?? $"{classSymbol.Name}Events";
        var sb = new StringBuilder();

        sb.AppendLine($@"    /// <summary>
    /// Helper class for creating domain events for {classSymbol.Name}.
    /// </summary>
    public static partial class {className}
    {{");

        foreach (var method in methods)
        {
            var helperMethod = GenerateHelperMethod(method, config, classSymbol);
            sb.AppendLine(helperMethod);
        }

        sb.AppendLine("    }");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a helper method for creating a specific event.
    /// </summary>
    /// <param name="method">The method symbol.</param>
    /// <param name="config">The generation configuration.</param>
    /// <param name="classSymbol">The containing class symbol.</param>
    /// <returns>The generated helper method code.</returns>
    private static string GenerateHelperMethod(IMethodSymbol method, EventGenerationConfig config, INamedTypeSymbol classSymbol)
    {
        var eventName = DeriveEventName(method, config);
        var methodName = $"Create{method.Name}{config.EventSuffix}";

        var eventAttribute = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "DomainEventAttribute" ||
                                a.AttributeClass?.ToDisplayString() == "Mesch.DomainEvents.DomainEventAttribute");

        if (eventAttribute == null)
        {
            return string.Empty;
        }

        // Get configuration from attribute
        var includeParameters = GetStringArrayFromAttribute(eventAttribute, "IncludeParameters");
        var excludeParameters = GetStringArrayFromAttribute(eventAttribute, "ExcludeParameters");
        var includeAggregateProperties = GetStringArrayFromAttribute(eventAttribute, "IncludeAggregateProperties");

        var sb = new StringBuilder();

        // Generate method signature
        sb.AppendLine($@"        /// <summary>
        /// Creates a {eventName} for the {method.Name} operation.
        /// </summary>");

        // Generate parameters
        var parameters = new List<string>();
        var assignments = new List<string>();

        // Always include aggregate ID
        parameters.Add("string aggregateId");
        assignments.Add("            AggregateId = aggregateId");

        sb.AppendLine("        /// <param name=\"aggregateId\">The ID of the aggregate.</param>");

        // Add method parameters
        foreach (var parameter in method.Parameters)
        {
            bool shouldInclude = ShouldIncludeParameter(parameter.Name, includeParameters, excludeParameters);

            if (shouldInclude)
            {
                var parameterType = parameter.Type.ToDisplayString();
                var propertyName = CapitalizeFirstLetter(parameter.Name);

                parameters.Add($"{parameterType} {parameter.Name}");
                assignments.Add($"            {propertyName} = {parameter.Name}");

                sb.AppendLine($"        /// <param name=\"{parameter.Name}\">The {parameter.Name} parameter value.</param>");
            }
        }

        // Add aggregate properties
        if (includeAggregateProperties != null && includeAggregateProperties.Length > 0)
        {
            foreach (var propertyName in includeAggregateProperties)
            {
                var property = classSymbol.GetMembers()
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(p => p.Name == propertyName);

                if (property != null)
                {
                    var propertyType = property.Type.ToDisplayString();
                    var parameterName = $"current{propertyName}";

                    parameters.Add($"{propertyType} {parameterName}");
                    assignments.Add($"            {propertyName} = {parameterName}");

                    sb.AppendLine($"        /// <param name=\"{parameterName}\">The current {propertyName} value.</param>");
                }
            }
        }

        sb.AppendLine($"        /// <returns>A new {eventName} instance.</returns>");

        // Generate method
        sb.AppendLine($"        public static {eventName} {methodName}(");
        sb.AppendLine($"            {string.Join(",\n            ", parameters)})");
        sb.AppendLine("        {");
        sb.AppendLine($"            return new {eventName}");
        sb.AppendLine("            {");
        sb.AppendLine($"{string.Join(",\n", assignments)}");
        sb.AppendLine("            };");
        sb.AppendLine("        }");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Derives the event name from the method name and configuration.
    /// </summary>
    /// <param name="method">The method symbol.</param>
    /// <param name="config">The generation configuration.</param>
    /// <returns>The derived event name.</returns>
    private static string DeriveEventName(IMethodSymbol method, EventGenerationConfig config)
    {
        var eventAttribute = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "DomainEventAttribute" ||
                                a.AttributeClass?.ToDisplayString() == "Mesch.DomainEvents.DomainEventAttribute");

        if (eventAttribute != null)
        {
            var eventNameArg = eventAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "EventName");

            if (eventNameArg.Value.Value is string customEventName && !string.IsNullOrEmpty(customEventName))
            {
                return customEventName;
            }
        }

        return method.Name + config.EventSuffix;
    }

    /// <summary>
    /// Determines if a parameter should be included in the event based on include/exclude lists.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="includeParameters">The include list (null means include all).</param>
    /// <param name="excludeParameters">The exclude list.</param>
    /// <returns><c>true</c> if the parameter should be included; otherwise, <c>false</c>.</returns>
    private static bool ShouldIncludeParameter(string parameterName, string[]? includeParameters, string[]? excludeParameters)
    {
        if (includeParameters != null)
        {
            return includeParameters.Contains(parameterName);
        }

        if (excludeParameters != null)
        {
            return !excludeParameters.Contains(parameterName);
        }

        return true;
    }

    /// <summary>
    /// Gets a string array from an attribute's named arguments.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="argumentName">The argument name.</param>
    /// <returns>The string array or null if not found.</returns>
    private static string[]? GetStringArrayFromAttribute(AttributeData attribute, string argumentName)
    {
        var argument = attribute.NamedArguments
            .FirstOrDefault(arg => arg.Key == argumentName);

        if (argument.Value.IsNull || argument.Value.Values.IsEmpty)
        {
            return null;
        }

        return argument.Value.Values
            .Where(v => v.Value is string)
            .Select(v => (string)v.Value!)
            .ToArray();
    }

    /// <summary>
    /// Capitalizes the first letter of a string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The string with the first letter capitalized.</returns>
    private static string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return char.ToUpper(input[0]) + input.Substring(1);
    }
}