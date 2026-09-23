using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AsisyaApi.Infrastructure.Persistence;

internal static class PostgresErrors
{
    public static bool IsUniqueViolation(this DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
