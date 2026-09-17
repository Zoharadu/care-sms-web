using HospitalSms.Application.Abstractions;
using HospitalSms.Application.Dtos;
using Microsoft.Extensions.Logging;
using System.DirectoryServices.AccountManagement;

namespace HospitalSms.Infrastructure.Identity
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly ILogger<ActiveDirectoryService> _logger;

        public ActiveDirectoryService(ILogger<ActiveDirectoryService> logger)
        {
            _logger = logger;
        }

        public UserDetails? GetUserFromAd(string rawIdentityString)
        {
            if (string.IsNullOrEmpty(rawIdentityString) || !rawIdentityString.Contains("\\"))
            {
                _logger.LogWarning(
                    "Active Directory lookup was skipped because the Windows identity format was invalid.");
                return null;
            }

            if (!OperatingSystem.IsWindows())
            {
                _logger.LogWarning("Active Directory lookup is supported only on Windows.");
                return null;
            }

            string[] parts = rawIdentityString.Split('\\');
            string domainName = parts[0];
            string samAccountName = parts[1];

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, domainName))
                {
                    using (var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, samAccountName))
                    {
                        if (user != null)
                        {
                            _logger.LogInformation("Active Directory lookup completed successfully.");

                            return new UserDetails
                            {
                                FullName = user.DisplayName,
                                FirstName = user.GivenName,
                                LastName = user.Surname,
                                Email = user.EmailAddress
                            };
                        }
                    }
                }

                _logger.LogWarning("Active Directory lookup completed without finding a user.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Active Directory lookup failed.");
            }

            return null;
        }
    }
}
