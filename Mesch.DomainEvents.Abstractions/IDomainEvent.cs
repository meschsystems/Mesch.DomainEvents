using System;

namespace Mesch.DomainEvents
{
    /// <summary>
    /// Interface that all generated domain events implement.
    /// Provides common properties for event tracking and correlation.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        /// <value>The UTC timestamp of when the event was created.</value>
        DateTime Timestamp { get; }

        /// <summary>
        /// Gets the correlation ID for tracking related events.
        /// </summary>
        /// <value>A unique identifier for correlating related events across the system.</value>
        string CorrelationId { get; }

        /// <summary>
        /// Gets the type of event.
        /// </summary>
        /// <value>A string identifier for the type of event.</value>
        string EventType { get; }

        /// <summary>
        /// Gets the version of the event schema.
        /// </summary>
        /// <value>The schema version for backward compatibility.</value>
        int Version { get; }
    }

    /// <summary>
    /// Interface for events that relate to a specific aggregate.
    /// Extends <see cref="IDomainEvent"/> with aggregate-specific properties.
    /// </summary>
    public interface IAggregateEvent : IDomainEvent
    {
        /// <summary>
        /// Gets the ID of the aggregate this event relates to.
        /// </summary>
        /// <value>The unique identifier of the aggregate.</value>
        string AggregateId { get; }

        /// <summary>
        /// Gets the type of the aggregate.
        /// </summary>
        /// <value>The name of the aggregate type.</value>
        string AggregateType { get; }
    }
}