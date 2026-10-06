using Imova.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Imova.Infrastructure;

public sealed class PostgresDatabaseErrors : IDatabaseErrors
{
    public bool IsUniqueViolation(DbUpdateException exception, string indexName) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && string.Equals(postgres.ConstraintName, indexName, StringComparison.Ordinal);
}
