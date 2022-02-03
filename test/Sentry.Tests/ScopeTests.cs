using Sentry.Testing;

namespace Sentry.Tests;

public class ScopeTests
{
    private readonly Scope _sut = new();

    [Fact]
    public void OnEvaluate_FiresOnlyOnce()
    {
        var counter = 0;
        _sut.OnEvaluating += (_, _) => counter++;

        _sut.Evaluate();
        _sut.Evaluate();

        Assert.Equal(1, counter);
    }

    [Fact]
    public void OnEvaluate_NoEventHandler_DoesNotReevaluate()
    {
        var counter = 0;
        _sut.Evaluate();

        _sut.OnEvaluating += (_, _) => counter++;

        _sut.Evaluate();

        Assert.Equal(0, counter);
    }

    [Fact]
    public void OnEvaluate_EventHandlerThrows_LogsException()
    {
        // Arrange
        var logger = new InMemoryDiagnosticLogger();

        var scope = new Scope(new SentryOptions
        {
            DiagnosticLogger = logger,
            Debug = true
        });

        var exception = new InvalidOperationException("test");
        scope.OnEvaluating += (_, _) => throw exception;

        // Act
        scope.Evaluate();

        // Assert
        logger.Entries.Should().Contain(entry =>
            entry.Message == "Failed invoking event handler." &&
            entry.Exception == exception);
    }

    [Fact]
    public void OnEvaluate_EventHandlerThrows_DoesNotReevaluate()
    {
        var counter = 0;

        _sut.OnEvaluating += (_, _) =>
        {
            counter++;
            throw new InvalidOperationException("test");
        };

        _sut.Evaluate();
        _sut.Evaluate();

        Assert.Equal(1, counter);
    }

    [Fact]
    public void OnEvaluate_HasEvaluatedProperty_True()
    {
        Assert.False(_sut.HasEvaluated);
        _sut.Evaluate();
        Assert.True(_sut.HasEvaluated);
    }

    [Fact]
    public void Clone_NewScope_IncludesOptions()
    {
        var options = new SentryOptions();
        var sut = new Scope(options);

        var clone = sut.Clone();

        Assert.Same(options, clone.Options);
    }

    [Fact]
    public void Clone_CopiesFields()
    {
        _sut.Environment = "test";

        var clone = _sut.Clone();

        Assert.Equal(_sut.Environment, clone.Environment);
    }

    [Fact]
    public void TransactionName_TransactionNotStarted_NameIsSet()
    {
        // Arrange
        var scope = new Scope
        {
            // Act
            TransactionName = "foo"
        };

        // Assert
        scope.TransactionName.Should().Be("foo");
        scope.Transaction.Should().BeNull();
    }

    [Fact]
    public void TransactionName_TransactionStarted_NameIsSetAndOverwritten()
    {
        // Arrange
        var scope = new Scope
        {
            Transaction = new TransactionTracer(DisabledHub.Instance, "bar", "_"),
            // Act
            TransactionName = "foo"
        };

        // Assert
        scope.TransactionName.Should().Be("foo");
        scope.TransactionName.Should().Be(scope.Transaction?.Name);
    }

    [Fact]
    public void TransactionName_TransactionStarted_NameIsSetToNullCoercedToEmpty()
    {
        // Arrange
        var scope = new Scope
        {
            Transaction = new TransactionTracer(DisabledHub.Instance, "bar", "_"),
            // Act
            TransactionName = null
        };

        // Assert
        scope.TransactionName.Should().BeNullOrEmpty();
        scope.TransactionName.Should().Be(scope.Transaction?.Name);
    }

    [Fact]
    public void TransactionName_TransactionStarted_NameReturnsActualTransactionName()
    {
        // Arrange
        var scope = new Scope
        {
            TransactionName = "bar",
            // Act
            Transaction = new TransactionTracer(DisabledHub.Instance, "foo", "_")
        };

        // Assert
        scope.TransactionName.Should().Be("foo");
        scope.TransactionName.Should().Be(scope.Transaction?.Name);
    }

    [Fact]
    public void GetSpan_NoSpans_ReturnsTransaction()
    {
        // Arrange
        var scope = new Scope();
        var transaction = new TransactionTracer(DisabledHub.Instance, "foo", "_");
        scope.Transaction = transaction;

        // Act
        var span = scope.GetSpan();

        // Assert
        span.Should().Be(transaction);
    }

    [Fact]
    public void GetSpan_FinishedSpans_ReturnsTransaction()
    {
        // Arrange
        var scope = new Scope();

        var transaction = new TransactionTracer(DisabledHub.Instance, "foo", "_");
        transaction.StartChild("123").Finish();
        transaction.StartChild("456").Finish();

        scope.Transaction = transaction;

        // Act
        var span = scope.GetSpan();

        // Assert
        span.Should().Be(transaction);
    }

    [Fact]
    public void GetSpan_ActiveSpans_ReturnsSpan()
    {
        // Arrange
        var scope = new Scope();

        var transaction = new TransactionTracer(DisabledHub.Instance, "foo", "_");
        var activeSpan = transaction.StartChild("123");
        transaction.StartChild("456").Finish();

        scope.Transaction = transaction;

        // Act
        var span = scope.GetSpan();

        // Assert
        span.Should().Be(activeSpan);
    }

    [Fact]
    public void AddAttachment_AddAttachments()
    {
        // Arrange
        var scope = new Scope();
        var attachment = new Attachment(default, default, default, default);
        var attachment2 = new Attachment(default, default, default, default);

        // Act
        scope.AddAttachment(attachment);
        scope.AddAttachment(attachment2);

        // Assert
        scope.Attachments.Should().Contain(attachment, "Attachment was not found.");
        scope.Attachments.Should().Contain(attachment2, "Attachment2 was not found.");
    }

    [Fact]
    public void ClearAttachments_HasAttachments_EmptyList()
    {
        // Arrange
        var scope = new Scope();

        for (var i = 0; i < 5; i++)
        {
            scope.AddAttachment(new MemoryStream(1_000), Guid.NewGuid().ToString());
        }

        // Act
        scope.ClearAttachments();

        // Assert
        scope.Attachments.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, -2, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 1)]
    [InlineData(0, 2, 1)]
    [InlineData(1, 2, 2)]
    [InlineData(2, 2, 2)]
    public void AddBreadcrumb__AddBreadcrumb_RespectLimits(int initialCount, int maxBreadcrumbs, int expectedCount)
    {
        // Arrange
        var scope = new Scope(new SentryOptions { MaxBreadcrumbs = maxBreadcrumbs });

        for (var i = 0; i < initialCount; i++)
        {
            scope.AddBreadcrumb(new Breadcrumb());
        }

        // Act
        scope.AddBreadcrumb(new Breadcrumb());

        // Assert
        Assert.Equal(expectedCount, scope.Breadcrumbs.Count);
    }

    [Theory]
    [InlineData("123@123.com", null, null, true)]
    [InlineData("123@123.com", null, null, false)]
    [InlineData(null, "my name", null, true)]
    [InlineData(null, "my name", null, false)]
    [InlineData(null, null, "my id", true)]
    [InlineData(null, null, "my id", false)]
    public void SetUser_ObserverExist_ObserverUserInvokedIfEnabled(string email, string username, string id, bool observerEnable)
    {
        // Arrange
        var observer = NSubstitute.Substitute.For<IScopeObserver>();
        var scope = new Scope(new SentryOptions
        {
            ScopeObserver = observer,
            EnableScopeSync = observerEnable
        });
        var expectedEmail = observerEnable ? email : null;
        var expectedusername = observerEnable ? username : null;
        var expectedid = observerEnable ? id : null;
        var expectedCount = observerEnable ? 1 : 0;
        // Act
        if (email != null)
        {
            scope.User.Email = email;
        }
        else if (username != null)
        {
            scope.User.Username = username;
        }
        else
        {
            scope.User.Id = id;
        }

        // Assert
        observer.Received(expectedCount).SetUser(Arg.Is<User>(user => user.Email == expectedEmail));
        observer.Received(expectedCount).SetUser(Arg.Is<User>(user => user.Id == expectedid));
        observer.Received(expectedCount).SetUser(Arg.Is<User>(user => user.Username == expectedusername));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UserChanged_ObserverNull_Ignored(bool observerEnable)
    {
        // Arrange
        var scope = new Scope(new SentryOptions { EnableScopeSync = observerEnable });
        Exception exception = null;

        // Act
        try
        {
            scope.UserChanged.Invoke(null);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetTag_ObserverExist_ObserverSetsTagIfEnabled(bool observerEnable)
    {
        // Arrange
        var observer = Substitute.For<IScopeObserver>();
        var scope = new Scope(new SentryOptions
        {
            ScopeObserver = observer,
            EnableScopeSync = observerEnable
        });
        var expectedKey = "1234";
        var expectedValue = "5678";
        var expectedCount = observerEnable ? 1 : 0;

        // Act
        scope.SetTag(expectedKey, expectedValue);

        // Assert
        observer.Received(expectedCount).SetTag(Arg.Is(expectedKey), Arg.Is(expectedValue));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UnsetTag_ObserverExist_ObserverUnsetsTagIfEnabled(bool observerEnable)
    {
        // Arrange
        var observer = Substitute.For<IScopeObserver>();
        var scope = new Scope(new SentryOptions
        {
            ScopeObserver = observer,
            EnableScopeSync = observerEnable
        });
        var expectedKey = "1234";
        var expectedCount = observerEnable ? 1 : 0;

        // Act
        scope.UnsetTag(expectedKey);

        // Assert
        observer.Received(expectedCount).UnsetTag(Arg.Is(expectedKey));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetExtra_ObserverExist_ObserverSetsExtraIfEnabled(bool observerEnable)
    {
        // Arrange
        var observer = Substitute.For<IScopeObserver>();
        var scope = new Scope(new SentryOptions
        {
            ScopeObserver = observer,
            EnableScopeSync = observerEnable
        });
        var expectedKey = "1234";
        var expectedValue = "5678";
        var expectedCount = observerEnable ? 1 : 0;

        // Act
        scope.SetExtra(expectedKey, expectedValue);

        // Assert
        observer.Received(expectedCount).SetExtra(Arg.Is(expectedKey), Arg.Is(expectedValue));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddBreadcrumb_ObserverExist_ObserverAddsBreadcrumbIfEnabled(bool observerEnable)
    {
        // Arrange
        var observer = Substitute.For<IScopeObserver>();
        var scope = new Scope(new SentryOptions
        {
            ScopeObserver = observer,
            EnableScopeSync = observerEnable
        });
        var breadcrumb = new Breadcrumb(message: "1234");
        var expectedCount = observerEnable ? 2 : 0;

        // Act
        scope.AddBreadcrumb(breadcrumb);
        scope.AddBreadcrumb(breadcrumb);

        // Assert
        observer.Received(expectedCount).AddBreadcrumb(Arg.Is(breadcrumb));
    }
}
