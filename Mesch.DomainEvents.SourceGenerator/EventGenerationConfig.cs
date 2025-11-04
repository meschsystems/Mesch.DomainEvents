using Microsoft.CodeAnalysis;

namespace Mesch.DomainEvents;

/// <summary>
/// Configuration settings for event generation extracted from attributes.
/// </summary>
internal sealed class EventGenerationConfig
{
    /// <summary>
    /// Gets or sets the namespace where events will be generated.
    /// </summary>
    /// <value>The namespace for generated events.</value>
    public string EventNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the suffix to append to event names.
    /// </summary>
    /// <value>The suffix for event names.</value>
    public string EventSuffix { get; set; } = "Event";

    /// <summary>
    /// Gets or sets a value indicating whether to generate a static helper class for creating events.
    /// </summary>
    /// <value><c>true</c> to generate helper class; otherwise, <c>false</c>.</value>
    public bool GenerateHelperClass { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the helper class.
    /// </summary>
    /// <value>The name of the helper class.</value>
    public string? HelperClassName { get; set; }
}