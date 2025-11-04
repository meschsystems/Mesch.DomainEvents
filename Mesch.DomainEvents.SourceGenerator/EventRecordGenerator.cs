using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mesch.DomainEvents;

/// <summary>
/// Generates event record classes for domain methods.
/// </summary>
internal static class EventRecordGenerator
{
    /// <summary>
    /// Generates event records for all methods with domain event attributes.
    /// </summary>
    /// <param name="methods">The methods to generate events for.</param>
    /// <param name="config">The generation configuration.</param>
    /// <param name="classSymbol">The containing class symbol.</param>
    /// <returns>The generated event record code.</returns>
    public static string GenerateEventRecords(List<IMethodSymbol> methods, EventGenerationConfig config, INamedTypeSymbol classSymbol)
    {
        var sb = new StringBuilder();

        foreach (var method in methods)
        {
            var eventName = DeriveEventName(method, config);
            var eventProperties = GenerateEventProperties(method, classSymbol);

            sb.AppendLine($@"    /// <summary>
    /// Event generated for {classSymbol.Name}.{method.Name} method.
    /// </summary>
    public record {eventName} : IAggregateEvent
    {{
{eventProperties}
        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        /// <value>The UTC timestamp of when the event was created.</value>
        public DateTime Timestamp {{ get; init; }} = DateTime.UtcNow;

        /// <summary>
        /// Gets the correlation ID for tracking related events.
        /// </summary>
        /// <value>A unique identifier for correlating related events across the system.</value>
        public string CorrelationId {{ get; init; }} = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets the type of event.
        /// </summary>
        /// <value>A string identifier for the type of event.</value>
        public string EventType {{ get; init; }} = ""{eventName}"";

        /// <summary>
        /// Gets the version of the event schema.
        /// </summary>
        /// <value>The schema version for backward compatibility.</value>
        public int Version {{ get; init; }} = 1;

        /// <summary>
        /// Gets the ID of the aggregate this event relates to.
        /// </summary>
        /// <value>The unique identifier of the aggregate.</value>
        public string AggregateId {{ get; init; }} = string.Empty;

        /// <summary>
        /// Gets the type of the aggregate.
        /// </summary>
        /// <value>The name of the aggregate type.</value>
        public string AggregateType {{ get; init; }} = ""{classSymbol.Name}"";
    }}");
            sb.AppendLine();
        }

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
    /// Generates properties for an event record based on method parameters and configuration.
    /// </summary>
    /// <param name="method">The method symbol.</param>
    /// <param name="classSymbol">The containing class symbol.</param>
    /// <returns>The generated properties code.</returns>
    private static string GenerateEventProperties(IMethodSymbol method, INamedTypeSymbol classSymbol)
    {
        var sb = new StringBuilder();

        var eventAttribute = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "DomainEventAttribute" ||
                                a.AttributeClass?.ToDisplayString() == "Mesch.DomainEvents.DomainEventAttribute");

        if (eventAttribute == null)
        {
            return string.Empty;
        }

        // Get include/exclude parameter lists
        var includeParameters = GetStringArrayFromAttribute(eventAttribute, "IncludeParameters");
        var excludeParameters = GetStringArrayFromAttribute(eventAttribute, "ExcludeParameters");
        var includeAggregateProperties = GetStringArrayFromAttribute(eventAttribute, "IncludeAggregateProperties");

        // Generate properties for method parameters
        foreach (var parameter in method.Parameters)
        {
            bool shouldInclude = ShouldIncludeParameter(parameter.Name, includeParameters, excludeParameters);

            if (shouldInclude)
            {
                var propertyName = CapitalizeFirstLetter(parameter.Name);
                var propertyType = parameter.Type.ToDisplayString();

                sb.AppendLine($@"        /// <summary>
        /// Gets the {parameter.Name} parameter value.
        /// </summary>
        /// <value>The {parameter.Name} value.</value>
        public {propertyType} {propertyName} {{ get; init; }} = default!;");
                sb.AppendLine();
            }
        }

        // Generate properties for aggregate properties if specified
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

                    sb.AppendLine($@"        /// <summary>
        /// Gets the current {propertyName} value from the aggregate.
        /// </summary>
        /// <value>The current {propertyName} value.</value>
        public {propertyType} {propertyName} {{ get; init; }} = default!;");
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
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