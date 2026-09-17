using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Helpers;

internal static class DatabaseExceptionHelper
{
    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
    }
}
