namespace Sentry.Tests;

public class HubExtensionsTests
{
    public IHub Sut { get; set; } = Substitute.For<IHub>();
    public Scope Scope { get; set; } = new();

    public HubExtensionsTests()
    {
        Sut.SubstituteConfigureScope(Scope);
    }

    [Fact]
    public void PushAndLockScope_PushesNewScope()
    {
        _ = Sut.PushAndLockScope();

        _ = Sut.Received(1).PushScope();
    }

    [Fact]
    public void PushAndLockScope_Disposed_DisposesInnerScope()
    {
        var disposable = Substitute.For<IDisposable>();
        _ = Sut.PushScope().Returns(disposable);

        var actual = Sut.PushAndLockScope();
        actual.Dispose();

        disposable.Received(1).Dispose();
    }

    [Fact]
    public void PushAndLockScope_CreatedScopeIsLocked()
    {
        _ = Sut.PushAndLockScope();

        Assert.True(Scope.Locked);
    }

    [Fact]
    public void LockScope_LocksScope()
    {
        Sut.LockScope();

        Assert.True(Scope.Locked);
    }

    [Fact]
    public void UnlockScope_UnlocksScope()
    {
        Sut.LockScope();

        Sut.UnlockScope();

        Assert.False(Scope.Locked);
    }

    [Fact]
    public void CaptureExceptionInternal_NoExplicitFlag_DefaultsHandledFalse()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var ex = new Exception("unhandled");
        SentryEvent captured = null;
        hub.CaptureEvent(Arg.Do<SentryEvent>(e => captured = e)).Returns(new SentryId());

        // Act
        hub.CaptureExceptionInternal(ex);

        // Assert
        Assert.Equal(false, ex.Data[Mechanism.HandledKey]);
    }

    [Fact]
    public void CaptureExceptionInternal_ExplicitFlag_NotOverridden()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var ex = new Exception("handled by integration");
        ex.Data[Mechanism.HandledKey] = true;

        // Act
        hub.CaptureExceptionInternal(ex);

        // Assert
        Assert.Equal(true, ex.Data[Mechanism.HandledKey]);
    }

    [Fact]
    public void CaptureException_ExplicitHandledFalse_SetsFlag()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        var ex = new Exception("caught and rethrown");

        // Act
        hub.CaptureException(ex, handled: false);

        // Assert
        Assert.Equal(false, ex.Data[Mechanism.HandledKey]);
    }

    [Fact]
    public void CaptureException_ExplicitHandledTrue_SetsFlag()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        var ex = new Exception("caught");

        // Act
        hub.CaptureException(ex, handled: true);

        // Assert
        Assert.Equal(true, ex.Data[Mechanism.HandledKey]);
    }

    [Fact]
    public void CaptureException_ScopeCallbackExplicitHandledTrue_SetsFlag()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        var ex = new Exception("caught");

        // Act
        hub.CaptureException(ex, _ => { }, handled: true);

        // Assert
        Assert.Equal(true, ex.Data[Mechanism.HandledKey]);
    }

    [Fact]
    public void CaptureException_ScopeCallbackExplicitHandledFalse_SetsFlag()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        var ex = new Exception("caught and rethrown");

        // Act
        hub.CaptureException(ex, _ => { }, handled: false);

        // Assert
        Assert.Equal(false, ex.Data[Mechanism.HandledKey]);
    }

    [Fact]
    public void CaptureException_DisabledHubExplicitHandled_DoesNotMutateExceptionData()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(false);
        var ex = new Exception("captured while disabled");

        // Act
        var id = hub.CaptureException(ex, handled: false);

        // Assert
        Assert.False(ex.Data.Contains(Mechanism.HandledKey));
        Assert.Equal(SentryId.Empty, id);
        hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void CaptureException_DisabledHubScopeCallbackExplicitHandled_DoesNotMutateExceptionData()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(false);
        var ex = new Exception("captured while disabled");

        // Act
        var id = hub.CaptureException(ex, _ => { }, handled: false);

        // Assert
        Assert.False(ex.Data.Contains(Mechanism.HandledKey));
        Assert.Equal(SentryId.Empty, id);
        hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void CaptureException_NoHandledArgument_PreservesPresetFlag()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        var ex = new Exception("preset mechanism");
        ex.SetSentryMechanism("MyHandler", handled: false);

        // Act
        hub.CaptureException(ex);

        // Assert
        Assert.Equal(false, ex.Data[Mechanism.HandledKey]);
    }

    [Fact]
    public void CaptureException_ScopeCallbackNoHandledArgument_PreservesPresetFlag()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        var ex = new Exception("preset mechanism");
        ex.SetSentryMechanism("MyHandler", handled: false);

        // Act
        hub.CaptureException(ex, _ => { });

        // Assert
        Assert.Equal(false, ex.Data[Mechanism.HandledKey]);
    }

    [Fact]
    public void AddBreadcrumb_MinimalArguments_CreatesBreadcrumb()
    {
        const string expectedMessage = "message";
        Sut.AddBreadcrumb(expectedMessage);

        _ = Assert.Single(Scope.Breadcrumbs);
        var crumb = Scope.Breadcrumbs.Single();
        Assert.Equal(expectedMessage, crumb.Message);
        Assert.Null(crumb.Category);
        Assert.Null(crumb.Data);
        Assert.Null(crumb.Type);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.NotEqual(default, crumb.Timestamp);
    }

    [Fact]
    public void AddBreadcrumb_AllFields_CreatesBreadcrumb()
    {
        var expectedTimestamp = DateTimeOffset.MaxValue;
        var clock = new MockClock(expectedTimestamp);

        const string expectedMessage = "message";
        const string expectedType = "type";
        const string expectedCategory = "category";
        var expectedData = new Dictionary<string, string>
        {
            {"Key", null},
            {"Key2", "value2"},
        };
        const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Fatal;

        Sut.AddBreadcrumb(
            clock,
            expectedMessage,
            expectedCategory,
            expectedType,
            expectedData,
            expectedLevel);

        _ = Assert.Single(Scope.Breadcrumbs);
        var crumb = Scope.Breadcrumbs.First();

        Assert.Equal(expectedMessage, crumb.Message);
        Assert.Equal(expectedType, crumb.Type);
        Assert.Equal(expectedCategory, crumb.Category);
        Assert.Equal(expectedLevel, crumb.Level);
        Assert.Equal(expectedData.Count, crumb.Data.Count);
        Assert.Equal(expectedData.ToImmutableDictionary(), crumb.Data);
        Assert.Equal(expectedMessage, crumb.Message);
        Assert.Equal(expectedTimestamp, crumb.Timestamp);
    }

    [Fact]
    public void GetTraceIdAndSpanId_WithActiveSpan_HasBothTraceIdAndSpanId()
    {
        // Arrange
        var span = Substitute.For<ISpan>();
        span.TraceId.Returns(SentryId.Create());
        span.SpanId.Returns(Sentry.SpanId.Create());

        var hub = Substitute.For<IHub>();
        hub.GetSpan().Returns(span);

        // Act
        hub.GetTraceIdAndSpanId(out var traceId, out var spanId);

        // Assert
        traceId.Should().Be(span.TraceId);
        spanId.Should().Be(span.SpanId);
    }

    [Fact]
    public void GetTraceIdAndSpanId_WithoutActiveSpan_HasOnlyTraceIdButNoSpanId()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.GetSpan().Returns((ISpan)null);

        var scope = new Scope();
        hub.SubstituteConfigureScope(scope);

        // Act
        hub.GetTraceIdAndSpanId(out var traceId, out var spanId);

        // Assert
        traceId.Should().Be(scope.PropagationContext.TraceId);
        spanId.Should().BeNull();
    }

    [Fact]
    public void GetTraceIdAndSpanId_WithoutIds_ShouldBeUnreachable()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.GetSpan().Returns((ISpan)null);

        // Act
        hub.GetTraceIdAndSpanId(out var traceId, out var spanId);

        // Assert
        traceId.Should().Be(SentryId.Empty);
        spanId.Should().BeNull();
    }
}
