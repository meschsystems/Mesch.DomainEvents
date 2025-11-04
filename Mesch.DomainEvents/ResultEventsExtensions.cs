using System.Runtime.CompilerServices;

namespace Mesch.DomainEvents;

/// <summary>
/// Represents an error result.
/// </summary>
public sealed record Error(string Message, Exception? Exception = null);

/// <summary>
/// Represents a successful result without a specific value.
/// </summary>
public sealed record Success;

/// <summary>
/// Represents a result that can either be a success value or an error.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;
    private readonly bool _isSuccess;

    private Result(T value)
    {
        _value = value;
        _error = null;
        _isSuccess = true;
    }

    private Result(Error error)
    {
        _value = default;
        _error = error ?? throw new ArgumentNullException(nameof(error));
        _isSuccess = false;
    }

    /// <summary>
    /// Gets whether this result represents a success.
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// Gets whether this result represents an error.
    /// </summary>
    public bool IsError => !_isSuccess;

    /// <summary>
    /// Gets the success value. Throws if the result is an error.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when attempting to access value on an error result.</exception>
    public T Value => _isSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result. Check IsSuccess before accessing Value.");

    /// <summary>
    /// Gets the error. Throws if the result is a success.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when attempting to access error on a success result.</exception>
    public Error Error => !_isSuccess
        ? _error!
        : throw new InvalidOperationException("Cannot access error of a successful result. Check IsError before accessing Error.");

    /// <summary>
    /// Tries to get the success value.
    /// </summary>
    /// <param name="value">The success value if the result is successful, otherwise default.</param>
    /// <returns>True if the result is successful, otherwise false.</returns>
    public bool TryGetValue(out T? value)
    {
        value = _isSuccess ? _value : default;
        return _isSuccess;
    }

    /// <summary>
    /// Tries to get the error.
    /// </summary>
    /// <param name="error">The error if the result is an error, otherwise null.</param>
    /// <returns>True if the result is an error, otherwise false.</returns>
    public bool TryGetError(out Error? error)
    {
        error = !_isSuccess ? _error : null;
        return !_isSuccess;
    }

    /// <summary>
    /// Executes one of two functions depending on whether this is a success or error result.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onError)
    {
        return _isSuccess ? onSuccess(_value!) : onError(_error!);
    }

    /// <summary>
    /// Executes one of two actions depending on whether this is a success or error result.
    /// </summary>
    public void Match(Action<T> onSuccess, Action<Error> onError)
    {
        if (_isSuccess)
        {
            onSuccess(_value!);
        }
        else
        {
            onError(_error!);
        }
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates an error result.
    /// </summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>
    /// Implicitly converts an error to an error result.
    /// </summary>
    public static implicit operator Result<T>(Error error) => new(error);
}

/// <summary>
/// Thread-safe storage for events attached to domain objects.
/// Uses ConditionalWeakTable to avoid memory leaks - events are automatically
/// garbage collected when the associated object is collected.
/// </summary>
internal static class EventStorage
{
    private static readonly ConditionalWeakTable<object, List<object>> _eventsByObject = new();

    /// <summary>
    /// Attaches events to an object instance.
    /// </summary>
    public static void AttachEvents(object obj, IEnumerable<object> events)
    {
        if (obj is null || events is null)
        {
            return;
        }

        var eventList = _eventsByObject.GetOrCreateValue(obj);

        lock (eventList)
        {
            eventList.AddRange(events);
        }
    }

    /// <summary>
    /// Retrieves all events attached to an object instance.
    /// </summary>
    public static IReadOnlyList<object> GetEvents(object obj)
    {
        if (obj is null)
        {
            return Array.Empty<object>();
        }

        if (_eventsByObject.TryGetValue(obj, out var events))
        {
            lock (events)
            {
                return events.ToArray(); // Return defensive copy
            }
        }

        return Array.Empty<object>();
    }

    /// <summary>
    /// Clears all events attached to an object instance.
    /// </summary>
    public static void ClearEvents(object obj)
    {
        if (obj is null)
        {
            return;
        }

        if (_eventsByObject.TryGetValue(obj, out var events))
        {
            lock (events)
            {
                events.Clear();
            }
        }
    }
}

/// <summary>
/// Extensions for attaching and working with domain events on any value.
/// Provides a clean, fluent API for domain event management.
/// </summary>
public static class DomainEventExtensions
{
    /// <summary>
    /// Creates a successful result with the given value.
    /// </summary>
    public static Result<T> Ok<T>(T value) => Result<T>.Success(value);

    /// <summary>
    /// Creates a successful result with a success marker (for non-generic operations).
    /// </summary>
    public static Result<Success> Ok() => Result<Success>.Success(new Success());

    /// <summary>
    /// Creates an error result.
    /// </summary>
    public static Result<T> Fail<T>(string message, Exception? exception = null) =>
        Result<T>.Failure(new Error(message, exception));

    /// <summary>
    /// Creates a successful result with events attached.
    /// </summary>
    public static Result<T> OkWithEvents<T>(T value, params IDomainEvent[] events) where T : notnull
    {
        EventStorage.AttachEvents(value, events);
        return Result<T>.Success(value);
    }

    /// <summary>
    /// Creates a successful result with events attached (for non-generic operations).
    /// </summary>
    public static Result<Success> OkWithEvents(params IDomainEvent[] events)
    {
        var success = new Success();
        EventStorage.AttachEvents(success, events);
        return Result<Success>.Success(success);
    }

    /// <summary>
    /// Attaches domain events to any value and returns it as a result.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to attach events to.</param>
    /// <param name="events">The domain events to attach.</param>
    /// <returns>The value wrapped in a result.</returns>
    public static Result<T> WithEvents<T>(
        this T value,
        params IDomainEvent[] events) where T : notnull
    {
        EventStorage.AttachEvents(value, events);
        return value;
    }

    /// <summary>
    /// Attaches events to any value and returns it as a result.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to attach events to.</param>
    /// <param name="events">The events to attach.</param>
    /// <returns>The value wrapped in a result.</returns>
    public static Result<T> WithEvents<T>(
        this T value,
        params object[] events) where T : notnull
    {
        EventStorage.AttachEvents(value, events);
        return value;
    }

    /// <summary>
    /// Attaches events to a result (for chaining).
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to attach events to.</param>
    /// <param name="events">The domain events to attach.</param>
    /// <returns>The result with events attached.</returns>
    public static Result<T> WithEvents<T>(
        this Result<T> result,
        params IDomainEvent[] events) where T : notnull
    {
        return result.Match<Result<T>>(
            value =>
            {
                EventStorage.AttachEvents(value, events);
                return value;
            },
            error => error
        );
    }

    /// <summary>
    /// Attaches events to a result (for chaining).
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to attach events to.</param>
    /// <param name="events">The events to attach.</param>
    /// <returns>The result with events attached.</returns>
    public static Result<T> WithEvents<T>(
        this Result<T> result,
        params object[] events) where T : notnull
    {
        return result.Match<Result<T>>(
            value =>
            {
                EventStorage.AttachEvents(value, events);
                return value;
            },
            error => error
        );
    }

    /// <summary>
    /// Extracts all events from a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to extract events from.</param>
    /// <returns>A list of all events.</returns>
    public static IReadOnlyList<object> GetEvents<T>(this T value)
    {
        if (value is null)
        {
            return Array.Empty<object>();
        }

        return EventStorage.GetEvents(value);
    }

    /// <summary>
    /// Extracts all events from a result.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to extract events from.</param>
    /// <returns>A list of all events.</returns>
    public static IReadOnlyList<object> GetEvents<T>(this Result<T> result)
    {
        return result.Match(
            value => value.GetEvents(),
            error => Array.Empty<object>()
        );
    }

    /// <summary>
    /// Extracts domain events (implementing IDomainEvent) from a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to extract events from.</param>
    /// <returns>An enumerable of domain events.</returns>
    public static IEnumerable<IDomainEvent> GetDomainEvents<T>(this T value)
    {
        return value.GetEvents().OfType<IDomainEvent>();
    }

    /// <summary>
    /// Extracts domain events (implementing IDomainEvent) from a result.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to extract events from.</param>
    /// <returns>An enumerable of domain events.</returns>
    public static IEnumerable<IDomainEvent> GetDomainEvents<T>(this Result<T> result)
    {
        return result.GetEvents().OfType<IDomainEvent>();
    }

    /// <summary>
    /// Extracts events of a specific type from a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TEvent">The type of events to retrieve.</typeparam>
    /// <param name="value">The value to extract events from.</param>
    /// <returns>An enumerable of events of the specified type.</returns>
    public static IEnumerable<TEvent> GetEventsOfType<T, TEvent>(this T value)
    {
        return value.GetEvents().OfType<TEvent>();
    }

    /// <summary>
    /// Extracts events of a specific type from a result.
    /// </summary>
    /// <typeparam name="TResult">The type of the success value.</typeparam>
    /// <typeparam name="TEvent">The type of events to retrieve.</typeparam>
    /// <param name="result">The result to extract events from.</param>
    /// <returns>An enumerable of events of the specified type.</returns>
    public static IEnumerable<TEvent> GetEventsOfType<TResult, TEvent>(this Result<TResult> result)
    {
        return result.GetEvents().OfType<TEvent>();
    }

    /// <summary>
    /// Checks if a value has any events attached.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value has events; otherwise, <c>false</c>.</returns>
    public static bool HasEvents<T>(this T value)
    {
        return value.GetEvents().Count > 0;
    }

    /// <summary>
    /// Checks if a result has any events attached.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <returns><c>true</c> if the result contains events; otherwise, <c>false</c>.</returns>
    public static bool HasEvents<T>(this Result<T> result)
    {
        return result.Match(
            value => value.HasEvents(),
            error => false
        );
    }

    /// <summary>
    /// Checks if a value has events of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TEvent">The type of events to check for.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value contains events of the specified type; otherwise, <c>false</c>.</returns>
    public static bool HasEventsOfType<T, TEvent>(this T value)
    {
        return value.GetEventsOfType<T, TEvent>().Any();
    }

    /// <summary>
    /// Checks if a result has events of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TEvent">The type of events to check for.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <returns><c>true</c> if the result contains events of the specified type; otherwise, <c>false</c>.</returns>
    public static bool HasEventsOfType<T, TEvent>(this Result<T> result)
    {
        return result.GetEventsOfType<T, TEvent>().Any();
    }

    /// <summary>
    /// Clears all events from a value. Useful for testing.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to clear events from.</param>
    public static void ClearEvents<T>(this T value)
    {
        if (value is not null)
        {
            EventStorage.ClearEvents(value);
        }
    }
}

/// <summary>
/// Helper methods for creating results with events.
/// </summary>
public static class ResultEventsExtensions
{
    /// <summary>
    /// Creates a successful result with the given value.
    /// </summary>
    public static Result<T> Ok<T>(T value) => Result<T>.Success(value);

    /// <summary>
    /// Creates a successful result with a success marker (for non-generic operations).
    /// </summary>
    public static Result<Success> Ok() => Result<Success>.Success(new Success());

    /// <summary>
    /// Creates an error result.
    /// </summary>
    public static Result<T> Fail<T>(string message, Exception? exception = null) =>
        Result<T>.Failure(new Error(message, exception));

    /// <summary>
    /// Creates a successful result with events attached.
    /// </summary>
    public static Result<T> OkWithEvents<T>(T value, params IDomainEvent[] events) where T : notnull
    {
        EventStorage.AttachEvents(value, events);
        return Result<T>.Success(value);
    }

    /// <summary>
    /// Creates a successful result with events attached (for non-generic operations).
    /// </summary>
    public static Result<Success> OkWithEvents(params IDomainEvent[] events)
    {
        var success = new Success();
        EventStorage.AttachEvents(success, events);
        return Result<Success>.Success(success);
    }
}

