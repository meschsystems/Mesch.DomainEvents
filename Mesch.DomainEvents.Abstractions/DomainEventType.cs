namespace Mesch.DomainEvents
{
    /// <summary>
    /// Defines the types of domain events that can be generated.
    /// </summary>
    public enum DomainEventType
    {
        /// <summary>
        /// Command event - represents something that was requested to happen.
        /// </summary>
        Command,

        /// <summary>
        /// Created event - represents an aggregate that was created.
        /// </summary>
        Created,

        /// <summary>
        /// Updated event - represents an aggregate that was modified.
        /// </summary>
        Updated,

        /// <summary>
        /// Deleted event - represents an aggregate that was removed.
        /// </summary>
        Deleted,

        /// <summary>
        /// Custom event type - allows for domain-specific event types.
        /// </summary>
        Custom
    }
}