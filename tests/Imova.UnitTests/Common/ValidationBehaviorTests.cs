using FluentValidation;
using Imova.Application.Common.Behaviors;
using MediatR;

namespace Imova.UnitTests.Common;

public class ValidationBehaviorTests
{
    public record TestRequest(string Value) : IRequest<string>;

    private sealed class AlwaysFailsValidator : AbstractValidator<TestRequest>
    {
        public AlwaysFailsValidator()
        {
            RuleFor(r => r.Value).Must(_ => false).WithMessage("Always fails.");
        }
    }

    private sealed class AlwaysPassesValidator : AbstractValidator<TestRequest>
    {
    }

    private static Task<string> Next(CancellationToken cancellationToken) => Task.FromResult("handled");

    [Fact]
    public async Task Handle_WithNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);

        var result = await behavior.Handle(new TestRequest("anything"), Next, CancellationToken.None);

        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Handle_WithPassingValidator_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new AlwaysPassesValidator()]);

        var result = await behavior.Handle(new TestRequest("anything"), Next, CancellationToken.None);

        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Handle_WithFailingValidator_ThrowsValidationExceptionAndDoesNotCallNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new AlwaysFailsValidator()]);
        var nextCalled = false;
        Task<string> Next(CancellationToken cancellationToken)
        {
            nextCalled = true;
            return Task.FromResult("handled");
        }

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new TestRequest("anything"), Next, CancellationToken.None));

        Assert.False(nextCalled);
        Assert.Contains(exception.Errors, e => e.ErrorMessage == "Always fails.");
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_AggregatesFailuresFromAll()
    {
        var secondValidator = new InlineValidator<TestRequest>();
        secondValidator.RuleFor(r => r.Value).Must(_ => false).WithMessage("Second failure.");

        var behavior = new ValidationBehavior<TestRequest, string>([new AlwaysFailsValidator(), secondValidator]);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new TestRequest("anything"), Next, CancellationToken.None));

        Assert.Contains(exception.Errors, e => e.ErrorMessage == "Always fails.");
        Assert.Contains(exception.Errors, e => e.ErrorMessage == "Second failure.");
    }
}
