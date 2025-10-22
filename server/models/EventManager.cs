using Microsoft.AspNetCore.Identity;
using e_Vent.tools;

namespace e_Vent.models;

public class EventManager: IdentityUser
{
    public required string OrganizationName { get; set; }
    public PremiumTiers Plan { get; set; }
}
