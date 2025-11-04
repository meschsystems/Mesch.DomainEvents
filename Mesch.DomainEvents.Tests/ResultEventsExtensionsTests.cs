using Mesch.DomainEvents;
using Xunit;

namespace Mesch.DomainEvents.Tests;

public class ResultEventsExtensionsTests
{
    private record TestEvent(string Message) : IDomainEvent
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
        public string EventType { get; init; } = nameof(TestEvent);
        public int Version { get; init; } = 1;
    }

    private record TestAggregateEvent(string AggregateId, string Message) : IAggregateEvent
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
        public string EventType { get; init; } = nameof(TestAggregateEvent);
        public int Version { get; init; } = 1;
        public string AggregateType { get; init; } = "TestAggregate";
    }

    [Fact]
    public void Ok_CreatesSuccessResult()
    {
        // Arrange & Act
        var result = ResultEventsExtensions.Ok("test value");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Equal("test value", result.Value);
    }

    [Fact]
    public void Fail_CreatesErrorResult()
    {
        // Arrange & Act
        var result = ResultEventsExtensions.Fail<string>("error message");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsError);
        Assert.Equal("error message", result.Error.Message);
    }

    [Fact]
    public void OkWithEvents_CreatesSuccessResultWithEvents()
    {
        // Arrange
        var event1 = new TestEvent("Event 1");
        var event2 = new TestEvent("Event 2");
        var value = new { Value = "test" }; // Use a unique object

        // Act
        var result = ResultEventsExtensions.OkWithEvents(value, event1, event2);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.HasEvents());
        var events = result.GetEvents();
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public void WithEvents_AddsEventsToSuccessResult()
    {
        // Arrange
        var value = new { Value = "test2" }; // Use a unique object
        var result = ResultEventsExtensions.Ok(value);
        var event1 = new TestEvent("Event 1");

        // Act
        var resultWithEvents = result.WithEvents(event1);

        // Assert
        Assert.True(resultWithEvents.HasEvents());
        var events = resultWithEvents.GetEvents();
        Assert.Single(events);
    }

    [Fact]
    public void WithEvents_DoesNotAddEventsToErrorResult()
    {
        // Arrange
        var result = ResultEventsExtensions.Fail<string>("error");
        var event1 = new TestEvent("Event 1");

        // Act
        var resultWithEvents = result.WithEvents(event1);

        // Assert
        Assert.False(resultWithEvents.HasEvents());
        Assert.True(resultWithEvents.IsError);
    }

    [Fact]
    public void GetEventsOfType_ReturnsCorrectEventType()
    {
        // Arrange
        var event1 = new TestEvent("Event 1");
        var event2 = new TestAggregateEvent("123", "Aggregate Event");
        var value = new TestValueObject { Value = "test3" }; // Use a unique object with a proper type
        var result = ResultEventsExtensions.OkWithEvents(value, event1, event2);

        // Act
        var testEvents = result.GetEventsOfType<TestValueObject, TestEvent>();
        var aggregateEvents = result.GetEventsOfType<TestValueObject, TestAggregateEvent>();

        // Assert
        Assert.Single(testEvents);
        Assert.Single(aggregateEvents);
        Assert.Equal("Event 1", testEvents.First().Message);
        Assert.Equal("Aggregate Event", aggregateEvents.First().Message);
    }

    private class TestValueObject
    {
        public string Value { get; set; } = "";
    }

    [Fact]
    public void GetDomainEvents_ReturnsOnlyDomainEvents()
    {
        // Arrange
        var event1 = new TestEvent("Event 1");
        var event2 = new TestAggregateEvent("123", "Aggregate Event");
        var value = new { Value = "test4" }; // Use a unique object
        var result = ResultEventsExtensions.OkWithEvents(value, event1, event2);

        // Act
        var domainEvents = result.GetDomainEvents();

        // Assert
        Assert.Equal(2, domainEvents.Count());
        Assert.All(domainEvents, evt => Assert.IsAssignableFrom<IDomainEvent>(evt));
    }

    [Fact]
    public void HasEventsOfType_ReturnsTrueWhenEventTypeExists()
    {
        // Arrange
        var event1 = new TestEvent("Event 1");
        var value = new TestValueObject { Value = "test5" }; // Use a unique object with a proper type
        var result = ResultEventsExtensions.OkWithEvents(value, event1);

        // Act & Assert
        Assert.True(result.HasEventsOfType<TestValueObject, TestEvent>());
        Assert.False(result.HasEventsOfType<TestValueObject, TestAggregateEvent>());
    }

    [Fact]
    public void Value_ThrowsWhenResultIsError()
    {
        // Arrange
        var result = ResultEventsExtensions.Fail<string>("error");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Match_ExecutesCorrectBranch()
    {
        // Arrange
        var successResult = ResultEventsExtensions.Ok("success");
        var errorResult = ResultEventsExtensions.Fail<string>("error");

        // Act
        var successMatch = successResult.Match(
            success => $"Success: {success}",
            error => $"Error: {error.Message}"
        );

        var errorMatch = errorResult.Match(
            success => $"Success: {success}",
            error => $"Error: {error.Message}"
        );

        // Assert
        Assert.Equal("Success: success", successMatch);
        Assert.Equal("Error: error", errorMatch);
    }

    [Fact]
    public void WithEvents_ChainMultipleCalls()
    {
        // Arrange
        var event1 = new TestEvent("Event 1");
        var event2 = new TestEvent("Event 2");
        var event3 = new TestEvent("Event 3");
        var value = new { Value = "test6" }; // Use a unique object

        // Act
        var result = ResultEventsExtensions.Ok(value)
            .WithEvents(event1)
            .WithEvents(event2)
            .WithEvents(event3);

        // Assert
        Assert.Equal(3, result.GetEvents().Count);
    }

    [Fact]
    public void NonGenericResult_WorksCorrectly()
    {
        // Arrange
        var event1 = new TestEvent("Event 1");

        // Act
        var result = ResultEventsExtensions.Ok().WithEvents(event1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.HasEvents());
        Assert.Single(result.GetEvents());
    }

    [Fact]
    public void TryGetValue_ReturnsValueOnSuccess()
    {
        // Arrange
        var result = ResultEventsExtensions.Ok("test value");

        // Act
        var success = result.TryGetValue(out var value);

        // Assert
        Assert.True(success);
        Assert.Equal("test value", value);
    }

    [Fact]
    public void TryGetValue_ReturnsDefaultOnError()
    {
        // Arrange
        var result = ResultEventsExtensions.Fail<string>("error");

        // Act
        var success = result.TryGetValue(out var value);

        // Assert
        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetError_ReturnsErrorOnFailure()
    {
        // Arrange
        var result = ResultEventsExtensions.Fail<string>("error message");

        // Act
        var isError = result.TryGetError(out var error);

        // Assert
        Assert.True(isError);
        Assert.NotNull(error);
        Assert.Equal("error message", error.Message);
    }

    [Fact]
    public void TryGetError_ReturnsNullOnSuccess()
    {
        // Arrange
        var result = ResultEventsExtensions.Ok("test");

        // Act
        var isError = result.TryGetError(out var error);

        // Assert
        Assert.False(isError);
        Assert.Null(error);
    }

    [Fact]
    public void Match_VoidOverload_ExecutesCorrectAction()
    {
        // Arrange
        var successResult = ResultEventsExtensions.Ok("success");
        var errorResult = ResultEventsExtensions.Fail<string>("error");
        var successExecuted = false;
        var errorExecuted = false;

        // Act
        successResult.Match(
            value => { successExecuted = true; },
            error => { Assert.Fail("Should not execute error action"); }
        );

        errorResult.Match(
            value => { Assert.Fail("Should not execute success action"); },
            error => { errorExecuted = true; }
        );

        // Assert
        Assert.True(successExecuted);
        Assert.True(errorExecuted);
    }
}
