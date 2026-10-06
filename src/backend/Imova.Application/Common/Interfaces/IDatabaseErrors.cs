using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Common.Interfaces;

// Recognises what a failed save ran into, without Application knowing the database driver
// (Imova.Infrastructure/PostgresDatabaseErrors).
public interface IDatabaseErrors
{
    // The save broke the unique index `indexName` (e.g. "IX_Agencies_Slug").
    bool IsUniqueViolation(DbUpdateException exception, string indexName);
}
