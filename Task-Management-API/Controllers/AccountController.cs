using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Task_Management_Api.Application.DTO;
using Task_Management_Api.Application.Interfaces;
using Task_Management_API.Domain.Constants;
using Task_Management_API.Domain.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Task_Management_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _config;
        private readonly ILogger<AccountController> _logger;
        private readonly IAuthService _authService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            ILogger<AccountController> logger,
            IAuthService authService,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
            _authService = authService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromForm] UserRegister dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new ErrorResponse
                {
                    Message = "Validation failed.",
                });
            }

            string imagePath = null;
            if (dto.Image != null && dto.Image.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);
                var path = Path.Combine("wwwroot/images", fileName);

                using (var image = SixLabors.ImageSharp.Image.Load(dto.Image.OpenReadStream()))
                {
                    image.Mutate(x => x.Resize( 350, 270)); 

                    image.Save(path); 
                }

                imagePath = $"images/{fileName}";
            }


            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Country = dto.Country,
                ImagePath = imagePath
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var identityErrors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new ErrorResponse
                {
                    Message = "User registration failed.",
                    Errors = identityErrors
                });
            }

            await _userManager.AddClaimsAsync(user, new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
            });

            await _userManager.AddToRoleAsync(user, Roles.User);

            return Ok(new { Message = "User registered successfully."});
        }



        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLogin UserFromRequest)
        {
            if (UserFromRequest == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "User login data is null.",
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Validation failed.",
                });
            }

            ApplicationUser userFromDb = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.UserName == UserFromRequest.UserName);

            if (userFromDb == null || !await _userManager.CheckPasswordAsync(userFromDb, UserFromRequest.Password))
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "Login failed.",
                    Errors = new List<string> { "Invalid username or password." }
                });
            }

            var userClaims = await _userManager.GetClaimsAsync(userFromDb);
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userFromDb.Id),
                new Claim(ClaimTypes.Name, userFromDb.UserName),
            };

            var roles = await _userManager.GetRolesAsync(userFromDb);
            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }


            var signInKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:SecritKey"]));
            var credentials = new SigningCredentials(signInKey, SecurityAlgorithms.HmacSha256);

            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(30);
            var jwtToken = new JwtSecurityToken(
                audience: _config["JWT:AudienceIP"],
                issuer: _config["JWT:IssuerIP"],
                claims: authClaims,
                expires: accessTokenExpiration,
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserId = userFromDb.Id
            };

            userFromDb.RefreshTokens.Add(refreshToken);
            await _userManager.UpdateAsync(userFromDb);

            return Ok(new
            {
                token = accessToken,
                expiration = accessTokenExpiration,
                refreshToken = refreshToken.Token,
                refreshTokenExpiry = refreshToken.ExpiryDate
            });
        }

        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh([FromBody] AuthResultDTO request)
        {
            try
            {
                //Refresh token request received

                if (string.IsNullOrEmpty(request?.AccessToken) || string.IsNullOrEmpty(request?.RefreshToken))
                    return BadRequest("Access token and refresh token are required.");

                var principal = _authService.GetPrincipalFromExpiredToken(request.AccessToken);
                if (principal == null)
                    return BadRequest("Invalid access token.");

                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.Users
                    .Include(u => u.RefreshTokens)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return Unauthorized("User not found.");

                var storedRefreshToken = user.RefreshTokens?.FirstOrDefault(rt => rt.Token == request.RefreshToken);
                if (storedRefreshToken == null)
                    return Unauthorized("Invalid refresh token.");

                if (storedRefreshToken.ExpiryDate < DateTime.UtcNow)
                    return Unauthorized("Expired refresh token.");

                var result = await _authService.GenerateTokenAsync(user, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
