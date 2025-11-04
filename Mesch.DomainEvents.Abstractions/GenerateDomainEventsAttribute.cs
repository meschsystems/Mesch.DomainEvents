using System;

namespace Mesch.DomainEvents
{
    /// <summary>
    /// Marks a class as supporting automatic domain event generation.
    /// When applied to a class, the source generator will scan for methods marked with DomainEventAttribute
    /// and generate corresponding event records and helper classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GenerateDomainEventsAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the namespace where events will be generated.
        /// If null, uses the same namespace as the class with ".Events" suffix.
        /// </summary>
        /// <value>The namespace for generated events.</value>
        public string EventNamespace { get; set; }

        /// <summary>
        /// Gets or sets the suffix to append to event names.
        /// </summary>
        /// <value>The suffix for event names. Default is "Event".</value>
        public string EventSuffix { get; set; } = "Event";

        /// <summary>
        /// Gets or sets a value indicating whether to generate a static helper class for creating events.
        /// </summary>
        /// <value><c>true</c> to generate helper class; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool GenerateHelperClass { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the helper class.
        /// If null, uses the class name + "Events".
        /// </summary>
        /// <value>The name of the helper class.</value>
        public string HelperClassName { get; set; }
    }
}