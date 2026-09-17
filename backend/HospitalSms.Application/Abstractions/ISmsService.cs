using HospitalSms.Application.Dtos;

namespace HospitalSms.Application.Abstractions;

public interface ISmsService
{
    Task<IReadOnlyList<SmsTestPhoneDto>> GetActiveTestPhonesAsync(
        CancellationToken cancellationToken);

    Task QueueSmsAsync(
        int categoryId,
        int templateId,
        string languageId,
        string message,
        long phoneNumber,
        CancellationToken cancellationToken);
}
