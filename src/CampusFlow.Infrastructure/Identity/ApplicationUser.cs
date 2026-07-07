using Microsoft.AspNetCore.Identity;

namespace CampusFlow.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid InstitutionId { get; set; }
    public string FullName { get; set; } = string.Empty;
}