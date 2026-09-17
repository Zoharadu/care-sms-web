using HospitalSms.Application.Dtos;

namespace HospitalSms.Application.Abstractions;

public interface IActiveDirectoryService
{
    UserDetails? GetUserFromAd(string rawIdentityString);
}
