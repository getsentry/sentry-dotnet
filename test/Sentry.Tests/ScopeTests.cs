using System;
using System.IO;
using FluentAssertions;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests
{
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
                entry.Exception == exception
            );
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
            var scope = new Scope();

            // Act
            scope.TransactionName = "foo";

            // Assert
            scope.TransactionName.Should().Be("foo");
            scope.Transaction.Should().BeNull();
        }

        [Fact]
        public void TransactionName_TransactionStarted_NameIsSetAndOverwritten()
        {
            // Arrange
            var scope = new Scope();
            scope.Transaction = new TransactionTracer(DisabledHub.Instance, "bar", "_");

            // Act
            scope.TransactionName = "foo";

            // Assert
            scope.TransactionName.Should().Be("foo");
            scope.TransactionName.Should().Be(scope.Transaction?.Name);
        }

        [Fact]
        public void TransactionName_TransactionStarted_NameIsSetToNullCoercedToEmpty()
        {
            // Arrange
            var scope = new Scope();
            scope.Transaction = new TransactionTracer(DisabledHub.Instance, "bar", "_");

            // Act
            scope.TransactionName = null;

            // Assert
            scope.TransactionName.Should().BeNullOrEmpty();
            scope.TransactionName.Should().Be(scope.Transaction?.Name);
        }

        [Fact]
        public void TransactionName_TransactionStarted_NameReturnsActualTransactionName()
        {
            // Arrange
            var scope = new Scope();

            scope.TransactionName = "bar";

            // Act
            scope.Transaction = new TransactionTracer(DisabledHub.Instance, "foo", "_");

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
            //Arrange
            var scope = new Scope();
            var attachment = new Attachment(default, default, default, default);
            var attachment2 = new Attachment(default, default, default, default);

            //Act
            scope.AddAttachment(attachment);
            scope.AddAttachment(attachment2);

            //Assert
            scope.Attachments.Should().Contain(attachment, "Attachment was not found.");
            scope.Attachments.Should().Contain(attachment2, "Attachment2 was not found.");
        }

        [Fact]
        public void ClearAttachments_HasAttachments_EmptyList()
        {
            //Arrange
            var scope = new Scope();

            for (int i = 0; i < 5; i++)
            {
                scope.AddAttachment(new MemoryStream(1_000), Guid.NewGuid().ToString());
            }

            //Act
            scope.ClearAttachments();

            //Assert
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
            //Arrange
            var scope = new Scope(new SentryOptions() { MaxBreadcrumbs = maxBreadcrumbs });

            for (int i = 0; i < initialCount; i++)
            {
                scope.AddBreadcrumb(new Breadcrumb());
            }

            //Act
            scope.AddBreadcrumb(new Breadcrumb());

            //Assert
            Assert.Equal(expectedCount, scope.Breadcrumbs.Count);
        }

        [Theory]
        [InlineData("123@123.com", null, null)]
        [InlineData(null, "my name", null)]
        [InlineData(null, null, "my id")]
        public void SetUserEmail_ObserverExist_ObserverUserEmailSet(string email, string username, string id)
        {
            //Arrange
            var observer = NSubstitute.Substitute.For<IScopeObserver>();
            var scope = new Scope(new SentryOptions() { ScopeObserver = observer });

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
            Assert.Equal(email, observer.User.Email);
            Assert.Equal(id, observer.User.Id);
            Assert.Equal(username, observer.User.Username);
        }

        [Fact]
        public void SetUserEmail_ObserverExistWithUser_ObserverUserEmailChanged()
        {
            //Arrange
            var observer = Substitute.For<IScopeObserver>();
            observer.User = new User() { Email = "4321" };
            var scope = new Scope(new SentryOptions() { ScopeObserver = observer });

            // Act
            scope.User.Email = "1234";

            // Assert
            Assert.Equal(scope.User.Email, observer.User.Email);
        }

        [Fact]
        public void UserChanged_Observernull_Ignored()
        {
            //Arrange
            var scope = new Scope();
            Exception exception = null;

            // Act
            try
            {
                scope.UserChanged.Invoke();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void SetTag_ObserverExist_ObserverSetsTag()
        {
            //Arrange
            var observer = Substitute.For<IScopeObserver>();
            var scope = new Scope(new SentryOptions() { ScopeObserver = observer });
            var expectedKey = "1234";
            var expectedValue = "5678";

            // Act
            scope.SetTag(expectedKey, expectedValue);

            // Assert
            observer.Received(1).SetTag(Arg.Is(expectedKey), Arg.Is(expectedValue));
        }

        [Fact]
        public void UnsetTag_ObserverExist_ObserverUnsetsTag()
        {
            //Arrange
            var observer = Substitute.For<IScopeObserver>();
            var scope = new Scope(new SentryOptions() { ScopeObserver = observer });
            var expectedKey = "1234";

            // Act
            scope.UnsetTag(expectedKey);

            // Assert
            observer.Received(1).UnsetTag(Arg.Is(expectedKey));
        }

        [Fact]
        public void SetExtra_ObserverExist_ObserverSetsExtra()
        {
            //Arrange
            var observer = Substitute.For<IScopeObserver>();
            var scope = new Scope(new SentryOptions() { ScopeObserver = observer });
            var expectedKey = "1234";
            var expectedValue = "5678";

            // Act
            scope.SetExtra(expectedKey, expectedValue);

            // Assert
            observer.Received(1).SetExtra(Arg.Is(expectedKey), Arg.Is(expectedValue));
        }

        [Fact]
        public void AddBreadcrumb_ObserverExist_ObserverAddsBreadcrumb()
        {
            //Arrange
            var observer = Substitute.For<IScopeObserver>();
            var scope = new Scope(new SentryOptions() { ScopeObserver = observer });
            var breadcrumb = new Breadcrumb(message: "1234");

            // Act
            scope.AddBreadcrumb(breadcrumb);
            scope.AddBreadcrumb(breadcrumb);

            // Assert
            observer.Received(2).AddBreadcrumb(Arg.Is(breadcrumb));
        }
    }
}
