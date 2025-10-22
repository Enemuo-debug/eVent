using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using e_Vent.models;
using Microsoft.IdentityModel.Tokens;
using DotNetEnv;

namespace e_Vent.tools
{
    public class Token
    {
        private readonly string Key;
        private readonly string Issuer;
        private readonly string Audience;
        public Token()
        {
            Env.Load();
            Key = Environment.GetEnvironmentVariable("JWTKey") ?? string.Empty;
            Issuer = Environment.GetEnvironmentVariable("Issuer") ?? string.Empty;
            Audience = Environment.GetEnvironmentVariable("Audience") ?? string.Empty;
        }
        public string CreateToken (EventManager user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("Plan", user.Plan.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key ??string.Empty));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken
            (
                issuer: Environment.GetEnvironmentVariable("Issuer"),
                audience: Environment.GetEnvironmentVariable("Audience"),
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        public bool VerifyToken (string cookie, out string userEmail)
        {
            userEmail = "";
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(cookie);
                if (jwt == null)
                {
                    userEmail = "Not Authenticated";
                    return false;
                }

                // Check expiry
                if (jwt.ValidTo < DateTime.UtcNow) 
                {
                    userEmail = "Expired";
                    return false;
                }

                // Extract email from common claim types
                foreach (var claim in jwt.Claims)
                {
                    if (claim.Type == ClaimTypes.Email || claim.Type == JwtRegisteredClaimNames.Email || claim.Type == "email")
                    {
                        userEmail = claim.Value;
                        return true;
                    }
                }

                userEmail = "Email claim not found";
                return false;
            }
            catch (Exception)
            {
                userEmail = "Invalid token";
                return false;
            }
        }
    }
}
