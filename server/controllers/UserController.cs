using Microsoft.AspNetCore.Identity;
using e_Vent.models;
using Microsoft.AspNetCore.Mvc;
using e_Vent.dtos;
using e_Vent.tools;

namespace e_Vent.controllers;

[ApiController]
[Route("managers")]
public class UserController : ControllerBase
{
    #region Variables and constructor
    private readonly UserManager<EventManager> userManager;
    private readonly SignInManager<EventManager> signInManager;
    private readonly Token JwtToken;
    public UserController(UserManager<EventManager> _userManager, SignInManager<EventManager> _signInManager, Token _jwtToken)
    {
        userManager = _userManager;
        signInManager = _signInManager;
        JwtToken = _jwtToken;
    }
    #endregion

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateAccount model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = new EventManager
        {
            UserName = model.UserName,
            Email = model.Email,
            OrganizationName = model.OrganizationName,
            Plan = PremiumTiers.Free
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            return Ok(new { Status = "Success", Message = "User created successfully!" });
        }
        else
        {
            return BadRequest(new { Status = "Error", Message = "User creation failed! Please check user details and try again." });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if the user exists and the password is correct
        var user = await userManager.FindByNameAsync(model.UserName);
        if (user == null)
        {
            return Unauthorized(new { Status = "Error", Message = "Invalid login attempt." });
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, false);

        if (result.Succeeded)
        {
            // Create JWT token
            var token = JwtToken.CreateToken(user); 
            Response.Cookies.Append("EventManagerToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.Now.AddDays(1)
            });
            return Ok(new { Status = "Success", Message = "User logged in successfully!"});
        }
        else
        {
            return Unauthorized(new { Status = "Error", Message = "Invalid login attempt." });
        }
    }

    [HttpGet("details")]
    public async Task<IActionResult> GetUserDetails()
    {
        var cookie = Request.Cookies["EventManagerToken"];
        if (string.IsNullOrEmpty(cookie))
            return Unauthorized(new { Status = "Error", Message = "User is not authenticated." });

        // Verify the token using the Token tool
        var principal = JwtToken.VerifyToken(cookie, out string userEmail);
        if (!principal)
        {
            return Unauthorized(new { Status = "Error", Message = userEmail });
        }

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { Status = "Error", Message = "Token does not contain user email." });

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
        {
            return NotFound(new { Status = "Error", Message = "User not found." });
        }

        return Ok(new
        {
            user.UserName,
            user.Email,
            user.OrganizationName,
            Plan = user.Plan.ToString()
        });
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("EventManagerToken", new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = true,
            Path = "/"
        });

        return Ok(new { Status = "Success", Message = "User logged out successfully!" });
    }
}