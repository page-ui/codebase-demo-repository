using Microsoft.AspNetCore.Identity;

namespace Page.Ui.Domain.Auth.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
    }
}

