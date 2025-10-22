using e_Vent.models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace e_Vent.data;

public class ApplicationDbContext: IdentityDbContext<EventManager>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }
    // DB Sets go here
    public DbSet<Event> Events { get; set; }
    public DbSet<GeneralForm> GeneralForms { get; set; }
    
}