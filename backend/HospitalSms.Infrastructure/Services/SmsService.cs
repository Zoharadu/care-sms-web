using HospitalSms.Application.Abstractions;
using HospitalSms.Application.Dtos;
using HospitalSms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;

namespace HospitalSms.Infrastructure.Services;

public sealed class SmsService : ISmsService
{
    private const string StoredProcedureName = "dbo.sp_sms_dev_send_delta_queue_add";
    private const string ActiveTestPhonesQuery = """
        SELECT
            phone_num_male,
            [name]
        FROM [dbo].[sms_test_phone_whitelist]
        WHERE is_active = 1
        ORDER BY [name], phone_num_male;
        """;

    private readonly HospitalSmsDbContext _context;
    private readonly ILogger<SmsService> _logger;

    public SmsService(
        HospitalSmsDbContext context,
        ILogger<SmsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SmsTestPhoneDto>> GetActiveTestPhonesAsync(
        CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        var testPhones = new List<SmsTestPhoneDto>();

        await using var command = connection.CreateCommand();
        command.CommandText = ActiveTestPhonesQuery;
        command.CommandType = CommandType.Text;

        try
        {
            if (closeConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var phoneNumberOrdinal = reader.GetOrdinal("phone_num_male");
            var nameOrdinal = reader.GetOrdinal("name");

            while (await reader.ReadAsync(cancellationToken))
            {
                var phoneNumber = reader.GetInt64(phoneNumberOrdinal);
                var name = reader.IsDBNull(nameOrdinal)
                    ? string.Empty
                    : reader.GetString(nameOrdinal);

                testPhones.Add(new SmsTestPhoneDto
                {
                    PhoneNumber = phoneNumber.ToString(CultureInfo.InvariantCulture),
                    Name = name
                });
            }

            _logger.LogInformation(
                "Loaded {TestPhoneCount} active SMS test phone numbers.",
                testPhones.Count);

            return testPhones;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Loading active SMS test phone numbers was canceled.");
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Loading active SMS test phone numbers failed.");
            throw;
        }
        finally
        {
            if (closeConnection && connection.State != ConnectionState.Closed)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task QueueSmsAsync(
        int categoryId,
        int templateId,
        string languageId,
        string message,
        long phoneNumber,
        CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;

        await using var command = connection.CreateCommand();
        command.CommandText = StoredProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@category_id", SqlDbType.Int)
        {
            Value = categoryId
        });
        command.Parameters.Add(new SqlParameter("@template_id", SqlDbType.Int)
        {
            Value = templateId
        });
        command.Parameters.Add(new SqlParameter("@language_id", SqlDbType.Char, 2)
        {
            Value = languageId
        });
        command.Parameters.Add(new SqlParameter("@msg_text", SqlDbType.NVarChar, 1000)
        {
            Value = message
        });
        command.Parameters.Add(new SqlParameter("@phone_num", SqlDbType.BigInt)
        {
            Value = phoneNumber
        });

        try
        {
            if (closeConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await command.ExecuteNonQueryAsync(cancellationToken);

            var phoneDigitCount = phoneNumber
                .ToString(CultureInfo.InvariantCulture)
                .Length;
            _logger.LogInformation(
                "SMS request was queued for category {CategoryId}, template {TemplateId}, language {LanguageId}, and a recipient number containing {PhoneDigitCount} digits.",
                categoryId,
                templateId,
                languageId,
                phoneDigitCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Queueing an SMS request was canceled for category {CategoryId}, template {TemplateId}, and language {LanguageId}.",
                categoryId,
                templateId,
                languageId);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Queueing an SMS request failed for category {CategoryId}, template {TemplateId}, and language {LanguageId}.",
                categoryId,
                templateId,
                languageId);
            throw;
        }
        finally
        {
            if (closeConnection && connection.State != ConnectionState.Closed)
            {
                await connection.CloseAsync();
            }
        }
    }
}
