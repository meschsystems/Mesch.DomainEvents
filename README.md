# Mesch.DomainEvents

A source generator library that automatically creates domain event records from domain method signatures.

## Installation

```bash
dotnet add package Mesch.DomainEvents
```

## Usage

### Basic Setup

Mark domain classes with `[GenerateDomainEvents]` and methods with `[DomainEvent]`. The source generator creates event records and helper classes at compile time.

```csharp
using Mesch.DomainEvents;

[GenerateDomainEvents(EventNamespace = "MyApp.Events")]
public partial record Customer
{
    [DomainEvent(EventType = DomainEventType.Created)]
    public static Result<Customer> Create(string name, string email) { }
}
```

### Generated Output

The source generator produces:
- Event record implementing `IAggregateEvent`
- Static helper class with factory methods
- All events placed in the specified namespace

### Configuration

**Class-level configuration:**
- `EventNamespace` - Target namespace for generated events
- `EventSuffix` - Suffix appended to event names (default: "Event")
- `GenerateHelperClass` - Whether to generate factory methods (default: true)
- `HelperClassName` - Name of the helper class (default: "{ClassName}Events")

**Method-level configuration:**
- `EventName` - Custom event name (default: derived from method name)
- `EventType` - Type of domain operation (Created, Updated, Deleted, Command, Custom)
- `IncludeParameters` - Specific parameters to include
- `ExcludeParameters` - Parameters to exclude from event
- `IncludeAggregateProperties` - Current aggregate properties to include
- `IncludeResult` - Whether to include method return value

## Event Capture

Events are stored using a custom Result<T> type that provides a clean, type-safe approach to handling success and error cases.

### Adding Events

Events can be attached to results using several methods:

```csharp
// Using generated helper
var eventData = CustomerEvents.CreateCreateEvent(id, name, email);
return ResultEventsExtensions.Ok(customer).WithEvents(eventData);

// Multiple events
return result.WithEvents(event1, event2, event3);

// Create result with events directly
return ResultEventsExtensions.OkWithEvents(customer, eventData);
```

### Extracting Events

Events can be retrieved from results for persistence or processing:

```csharp
var result = Customer.Create("John", "john@example.com");

// Get all events
var allEvents = result.GetEvents();

// Get specific event types
var domainEvents = result.GetDomainEvents();
var createEvents = result.GetEventsOfType<CustomerCreateEvent>();

// Check for events
bool hasEvents = result.HasEvents();
bool hasCreateEvents = result.HasEventsOfType<CustomerCreateEvent>();
```

## Event Persistence

A typical persistence pattern:

```csharp
public async Task<Result<Customer>> CreateCustomerAsync(string name, string email)
{
    var result = Customer.Create(name, email);

    if (result.IsSuccess)
    {
        // Persist aggregate
        await repository.SaveAsync(result.Value);

        // Persist events
        var events = result.GetDomainEvents();
        await eventStore.SaveEventsAsync(events);

        // Or publish events
        foreach (var evt in events)
        {
            await eventBus.PublishAsync(evt);
        }
    }

    return result;
}
```

## Event Schema

Generated events implement `IAggregateEvent` with standard properties:
- `Timestamp` - Event creation time (UTC)
- `CorrelationId` - Unique identifier for event correlation
- `EventType` - String identifier for the event type
- `Version` - Schema version for compatibility
- `AggregateId` - ID of the related aggregate
- `AggregateType` - Type name of the aggregate

## Event Access API Reference

After calling `Result.Ok(tenant).WithEvents(eventData)`, callers can access events using a variety of extension methods.

## Basic Event Extraction

### `GetEvents()`
Returns all events as `List<object>`:
```csharp
var result = Tenant.Create("Acme", "acme", "admin@acme.com");
var allEvents = result.GetEvents();  // List<object>
```

### `GetDomainEvents()`
Returns events that implement `IDomainEvent`:
```csharp
var result = Tenant.Create("Acme", "acme", "admin@acme.com");
var domainEvents = result.GetDomainEvents();  // IEnumerable<IDomainEvent>

foreach (var evt in domainEvents)
{
    Console.WriteLine($"{evt.EventType} at {evt.Timestamp}");
    Console.WriteLine($"Correlation: {evt.CorrelationId}");
}
```

## Strongly-Typed Event Extraction

### `GetEventsOfType<TEvent>()`
Returns events of a specific type:
```csharp
var result = Tenant.Create("Acme", "acme", "admin@acme.com");

// Get specific event types
var createEvents = result.GetEventsOfType<TenantCreateEvent>();
var updateEvents = result.GetEventsOfType<TenantUpdateNameEvent>();

foreach (var createEvent in createEvents)
{
    Console.WriteLine($"Created: {createEvent.Name} ({createEvent.Subdomain})");
    Console.WriteLine($"Admin: {createEvent.AdministratorEmailAddress}");
}
```

## Event Checking

### `HasEvents()`
Check if any events exist:
```csharp
var result = Tenant.Create("Acme", "acme", "admin@acme.com");

if (result.HasEvents())
{
    Console.WriteLine("Operation generated events");
    await ProcessEventsAsync(result.GetDomainEvents());
}
```

### `HasEventsOfType<TEvent>()`
Check for specific event types:
```csharp
var result = Tenant.Create("Acme", "acme", "admin@acme.com");

if (result.HasEventsOfType<TenantCreateEvent>())
{
    var events = result.GetEventsOfType<TenantCreateEvent>();
    await HandleCreationEventsAsync(events);
}
```

## Common Usage Patterns

### Pattern 1: Basic Event Processing
```csharp
var result = Tenant.Create("Acme", "acme", "admin@acme.com");

if (result.IsSuccess && result.HasEvents())
{
    var events = result.GetDomainEvents();
    await _eventPublisher.PublishAsync(events);
}
```

### Pattern 2: Type-Specific Event Handling
```csharp
var result = Tenant.Create("Acme", "acme", "admin@acme.com");

if (result.IsSuccess)
{
    // Handle creation events
    var createEvents = result.GetEventsOfType<TenantCreateEvent>();
    foreach (var evt in createEvents)
    {
        await _notificationService.SendWelcomeEmailAsync(evt.AdministratorEmailAddress);
        await _auditService.LogTenantCreationAsync(evt);
    }
}
```

### Pattern 3: Pattern Matching
```csharp
var result = Tenant.Create("Acme", "acme", "admin@acme.com");

// Pattern matching on the result itself
await result.Match(
    async tenant =>
    {
        foreach (var evt in result.GetDomainEvents())
        {
            switch (evt)
            {
                case TenantCreateEvent createEvent:
                    await HandleTenantCreated(createEvent);
                    break;

                case TenantUpdateNameEvent updateEvent:
                    await HandleTenantNameUpdated(updateEvent);
                    break;

                default:
                    await HandleGenericEvent(evt);
                    break;
            }
        }
    },
    async error => await LogError(error)
);
```

### Pattern 4: Event Aggregation
```csharp
var allEvents = new List<IDomainEvent>();

var result1 = Tenant.Create("Acme", "acme", "admin@acme.com");
var result2 = result1.IsSuccess
    ? result1.Value.UpdateName("Acme Corp", PersonId.New())
    : ResultEventsExtensions.Fail<Tenant>("Failed");

if (result1.IsSuccess) allEvents.AddRange(result1.GetDomainEvents());
if (result2.IsSuccess) allEvents.AddRange(result2.GetDomainEvents());

// Process all events together
await _eventStore.SaveEventsAsync(allEvents);
```

### Pattern 5: API Controller Usage
```csharp
[HttpPost]
public async Task<IActionResult> CreateTenant(CreateTenantRequest request)
{
    var result = Tenant.Create(request.Name, request.Subdomain, request.AdminEmail);

    return result.Match<IActionResult>(
        tenant =>
        {
            // Extract events for background processing
            var events = result.GetDomainEvents();
            _backgroundJobService.EnqueueEventsAsync(events);

            return Ok(new
            {
                TenantId = tenant.Id,
                EventCount = events.Count()
            });
        },
        error => BadRequest(new { error.Message })
    );
}
```

## Event Properties Available

All generated events implement `IAggregateEvent` and provide:

### Common Properties (from IDomainEvent)
- `DateTime Timestamp` - When the event occurred
- `string CorrelationId` - Unique correlation identifier
- `string EventType` - Type of event (e.g., "TenantCreateEvent")
- `int Version` - Schema version for compatibility

### Aggregate Properties (from IAggregateEvent)
- `string AggregateId` - ID of the aggregate that generated the event
- `string AggregateType` - Type name of the aggregate (e.g., "Tenant")

### Generated Properties
- Domain-specific properties based on method parameters
- Aggregate properties (if configured with `IncludeAggregateProperties`)

## Integration with Event Infrastructure

### Event Store Integration
```csharp
public async Task SaveTenantAsync(Tenant tenant, IEnumerable<IDomainEvent> events)
{
    await _tenantRepository.SaveAsync(tenant);
    await _eventStore.AppendEventsAsync(tenant.Id.ToString(), events);
}
```

### Message Bus Integration
```csharp
public async Task PublishEventsAsync(IEnumerable<IDomainEvent> events)
{
    foreach (var evt in events)
    {
        await _messageBus.PublishAsync(evt.EventType, evt);
    }
}
```

### Read Model Updates
```csharp
public async Task UpdateReadModelsAsync(IEnumerable<IDomainEvent> events)
{
    foreach (var evt in events.OfType<TenantCreateEvent>())
    {
        await _tenantReadModelService.CreateAsync(new TenantReadModel
        {
            Id = evt.AggregateId,
            Name = evt.Name,
            Subdomain = evt.Subdomain,
            CreatedAt = evt.Timestamp
        });
    }
}
```

This API provides a clean, strongly-typed way to access and process domain events generated by your domain methods.

## Project Structure

The library consists of three components:
- `Mesch.DomainEvents` - Main library with extensions and interfaces
- `Mesch.DomainEvents.Abstractions` - Attributes and interfaces for source generator
- `Mesch.DomainEvents.SourceGenerator` - Compile-time code generation (packaged as analyzer)

All components are included in the single `Mesch.DomainEvents` package.

## Requirements

- .NET 8.0 or later
- C# 11.0 or later

## Result Type

The library uses a custom `Result<T>` type that provides:

### Type Safety
- Compile-time enforcement of error handling
- Pattern matching support via `Match<TResult>()` method
- Implicit conversions from values and errors

### API
- `Result<T>.Success(value)` - Create successful result
- `Result<T>.Failure(error)` - Create error result
- `result.IsSuccess` - Check if successful (property)
- `result.IsError` - Check if error (property)
- `result.Value` - Get value or throw (property)
- `result.Error` - Get error or throw (property)
- `result.TryGetValue(out value)` - Try to get value safely
- `result.TryGetError(out error)` - Try to get error safely
- `result.Match(onSuccess, onError)` - Pattern match (2 overloads: returning TResult and void)

### Benefits
- **Zero Dependencies**: No external packages required
- **Type Safety**: Strong typing ensures compile-time safety
- **Functional Approach**: Pattern matching and immutability
- **Performance**: Lightweight implementation with minimal allocations
- **Clean API**: Intuitive and easy to use