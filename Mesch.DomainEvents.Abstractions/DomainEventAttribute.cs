using System;

namespace Mesch.DomainEvents
{
    /// <summary>
    /// Marks a method as generating a domain event.
    /// When applied to a method, the source generator will create an event record
    /// based on the method signature and configuration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DomainEventAttribute : Attribute
    {
    /// <summary>
    /// Gets or sets the name of the event.
    /// If null, derives from method name using the configured event suffix.
    /// </summary>
    /// <value>The name of the event.</value>
    public string EventName { get; set; }

    /// <summary>
    /// Gets or sets the type of domain operation this represents.
    /// </summary>
    /// <value>The type of domain event. Default is Command.</value>
    public DomainEventType EventType { get; set; } = DomainEventType.Command;

    /// <summary>
    /// Gets or sets the parameters to include in the event.
    /// If null, includes all parameters except those specified in ExcludeParameters.
    /// </summary>
    /// <value>An array of parameter names to include.</value>
    public string[] IncludeParameters { get; set; }

    /// <summary>
    /// Gets or sets the parameters to exclude from the event.
    /// Only used when IncludeParameters is null.
    /// </summary>
    /// <value>An array of parameter names to exclude.</value>
    public string[] ExcludeParameters { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include the method result in the event.
    /// </summary>
    /// <value><c>true</c> to include the result; otherwise, <c>false</c>. Default is <c>false</c>.</value>
    public bool IncludeResult { get; set; } = false;

    /// <summary>
    /// Gets or sets the properties from the aggregate to include in the event.
    /// Useful for including current state information in update events.
    /// </summary>
    /// <value>An array of aggregate property names to include.</value>
    public string[] IncludeAggregateProperties { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically include aggregate ID property.
    /// </summary>
    /// <value><c>true</c> to include aggregate ID; otherwise, <c>false</c>. Default is <c>true</c>.</value>
    public bool IncludeAggregateId { get; set; } = true;
    }
}
