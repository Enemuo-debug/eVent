using System.ComponentModel.DataAnnotations;

namespace e_Vent.dtos;

public class CreateAccount
{
    [Required]
    [StringLength(15, MinimumLength = 5, ErrorMessage = "Username must be between 5 and 15 characters.")]
    public required string UserName { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }

    [Required]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public required string ConfirmPassword { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Organization name must be between 5 and 100 characters.")]
    public required string OrganizationName { get; set; }
}