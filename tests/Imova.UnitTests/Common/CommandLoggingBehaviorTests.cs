using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Behaviors;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Logging;
using Imova.UnitTests.TestSupport;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Imova.UnitTests.Common;

public class CommandLoggingBehaviorTests
{
    public record ApproveThingCommand(Guid ThingId, string Email, bool IsAdmin) : IRequest<ThingResult>, ILoggedCommand;

    public record MarkThingSeenCommand(Guid ThingId) : IRequest<ThingResult>;

    public record ThingResult(Guid Id, string Title, Owner Owner);

    public record Owner(Guid Id, string Name);

    private static readonly Guid ThingId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private readonly CapturingLoggerFactory _loggers = new();

    private Task<ThingResult> Run<TCommand>(TCommand command, Func<ThingResult> handler) where TCommand : IRequest<ThingResult> =>
        new CommandLoggingBehavior<TCommand, ThingResult>(_loggers).Handle(command, _ => Task.FromResult(handler()), CancellationToken.None);

    private static ThingResult Result() => new(ThingId, "Secret title", new Owner(OwnerId, "Ion Popescu"));

    [Fact]
    public async Task Success_IsOneEventLine_WithTheIdsOfTheCommandAndTheResult()
    {
        await Run(new ApproveThingCommand(ThingId, "ion@example.md", true), Result);

        var entry = Assert.Single(_loggers.Entries);
        Assert.Equal(AppEvents.Category, entry.Category);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.StartsWith("ApproveThing succeeded", entry.Message);
        Assert.Equal(ThingId, entry.Scope["ThingId"]);
        Assert.Equal(ThingId, entry.Scope["Id"]);
        Assert.Equal(OwnerId, entry.Scope["OwnerId"]);
    }

    [Fact]
    // Only ids: never the email, names or titles that commands and results also carry.
    public async Task NoPersonalDataGetsIn()
    {
        await Run(new ApproveThingCommand(ThingId, "ion@example.md", true), Result);

        var entry = Assert.Single(_loggers.Entries);
        var everything = entry.Message + string.Join(",", entry.Scope.Select(p => $"{p.Key}={p.Value}"));
        Assert.DoesNotContain("ion@example.md", everything);
        Assert.DoesNotContain("Ion Popescu", everything);
        Assert.DoesNotContain("Secret title", everything);
        Assert.All(entry.Scope.Values, value => Assert.IsType<Guid>(value));
    }

    [Fact]
    public async Task AForbiddenAction_IsAWarningWithItsCode_AndStillThrows()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            Run(new ApproveThingCommand(ThingId, "x", false), () => throw new ForbiddenAccessException("No.", "thing.notYours")));

        var entry = Assert.Single(_loggers.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("ApproveThing refused: thing.notYours", entry.Message);
        Assert.Equal(ThingId, entry.Scope["ThingId"]);
    }

    [Fact]
    public async Task AValidationRefusal_IsInformationWithItsCodes()
    {
        var failure = new ValidationFailure("Reason", "Required.") { ErrorCode = "thing.reasonRequired" };

        await Assert.ThrowsAsync<ValidationException>(() =>
            Run(new ApproveThingCommand(ThingId, "x", true), () => throw new ValidationException([failure])));

        var entry = Assert.Single(_loggers.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("refused: thing.reasonRequired", entry.Message);
    }

    [Fact]
    // A crash is the global exception handler's to log — not a second time here.
    public async Task AnUnexpectedException_IsNotLoggedHere()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Run(new ApproveThingCommand(ThingId, "x", true), () => throw new InvalidOperationException("boom")));

        Assert.Empty(_loggers.Entries);
    }

    [Fact]
    public async Task ACommandWithoutTheMarker_LogsNothing()
    {
        await Run(new MarkThingSeenCommand(ThingId), Result);

        Assert.Empty(_loggers.Entries);
    }

    [Theory]
    [InlineData(typeof(ApproveThingCommand), "ApproveThing")]
    [InlineData(typeof(Owner), "Owner")]
    public void NameOf_DropsTheCommandSuffix(Type type, string expected)
    {
        Assert.Equal(expected, AppEvents.NameOf(type));
    }

    [Fact]
    public void Ids_SkipEmptyGuidsAndCollections()
    {
        var ids = AppEvents.Ids(new { ListingId = Guid.Empty, PhotoIds = new[] { Guid.NewGuid() }, AgencyId = (Guid?)null, UserId = OwnerId });

        Assert.Equal(["UserId"], ids.Keys);
    }
}
