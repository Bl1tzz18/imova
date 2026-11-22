using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Logging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Common.Behaviors;

// One log line per ILoggedCommand (see AppEvents): "ApproveListing succeeded" with the ids involved,
// or "Login refused: auth.invalidCredentials" when it's turned down. Runs outside
// ValidationBehavior, so validation refusals are logged too (with their error codes). Unexpected
// exceptions are left to the global exception handler, which logs them already.
public class CommandLoggingBehavior<TRequest, TResponse>(ILoggerFactory loggerFactory)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger _logger = loggerFactory.CreateLogger(AppEvents.Category);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ILoggedCommand)
        {
            return await next(cancellationToken);
        }

        var name = AppEvents.NameOf(request.GetType());
        TResponse response;
        try
        {
            response = await next(cancellationToken);
        }
        catch (Exception ex) when (RefusalCode(ex) is { } code)
        {
            var ids = AppEvents.Ids(request);
            using (_logger.BeginScope(ids))
            {
                // A wrong password, a lockout or a forbidden action may be someone probing; a
                // validation refusal is usually just a typo.
                var level = ex is ValidationException ? LogLevel.Information : LogLevel.Warning;
                _logger.Log(level, "{Event} refused: {Code} ({Ids})", name, code, AppEvents.Describe(ids));
            }

            throw;
        }

        var resultIds = AppEvents.Ids(request, response);
        using (_logger.BeginScope(resultIds))
        {
            _logger.LogInformation("{Event} succeeded ({Ids})", name, AppEvents.Describe(resultIds));
        }

        return response;
    }

    private static string? RefusalCode(Exception ex) => ex switch
    {
        ValidationException e => string.Join(",", e.Errors.Select(f => f.ErrorCode).Distinct()),
        AuthenticationFailedException e => e.Code,
        ForbiddenAccessException e => e.Code,
        ConflictException e => e.Code,
        TooManyRequestsException e => e.Code,
        _ => null,
    };
}
