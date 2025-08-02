using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Task_Management_Api.Application.DTO;
using Task_Management_Api.Application.Interfaces;
using Task_Management_API.Domain.Models;
using Task_Management_API.Infrastructure.Data;

namespace Task_Management_API.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public AuthService(UserManager<ApplicationUser> userManager, IConfiguration config, AppDbContext context)
        {
            _userManager = userManager;
            _config = config;
            _context = context;
        }

        public async Task<AuthResultDTO> GenerateTokenAsync(ApplicationUser user, string ipAddress)
        {
            var accessToken = GenerateAccessToken(GetClaims(user));
            var refreshToken = GenerateRefreshToken();

            var existingToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (existingToken != null)
            {
                _context.RefreshTokens.Remove(existingToken);
            }

            // Fixed: Use default value if config is missing
            var refreshTokenDays = _config["JWT:RefreshTokenExpirationDays"];
            var expirationDays = !string.IsNullOrEmpty(refreshTokenDays) ? int.Parse(refreshTokenDays) : 7;

            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(expirationDays),
                CreatedDate = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            await _context.RefreshTokens.AddAsync(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return new AuthResultDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            try
            {
                // Fixed: Correct configuration key name
                var secretKey = _config["JWT:SecritKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    throw new InvalidOperationException("JWT:SecritKey is not configured in appsettings.json");
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _config["JWT:Issuer"],
                    audience: _config["JWT:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating access token: {ex.Message}", ex);
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var secretKey = _config["JWT:SecritKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    throw new InvalidOperationException("JWT:SecritKey is not configured");
                }

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidIssuer = _config["JWT:IssuerIP"],
                    ValidAudience = _config["JWT:AudienceIP"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:SecritKey"]!)),
                    ValidateLifetime = false
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    return null;

                return principal;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private List<Claim> GetClaims(ApplicationUser user)
        {
            return new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.UserName!)
            };
        }

        private double GetAccessTokenExpirationMinutes()
        {
            var expirationMinutes = _config["JWT:AccessTokenExpirationMinutes"];
            return !string.IsNullOrEmpty(expirationMinutes) ? double.Parse(expirationMinutes) : 60.0;
        }
    }
}