using Microsoft.AspNetCore.Authorization;
using ReemRPG.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Added for logging
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ReemRPG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger; // Inject ILogger

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AccountController> logger) // Injected ILogger
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger; // Assign logger
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(AuthModel model)
        {
            _logger.LogInformation("Attempting to register a new user: {Email}", model.Email);

            var user = new IdentityUser { UserName = model.Email, Email = model.Email };

            // creates a new user in database: hashes and salts password, saves user and returns result-
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Registration failed for {Email}: {Errors}",
                    model.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors);
            }

            // Generate email verification token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            _logger.LogInformation("Generated email verification token: {Token}", token);

            // Create verification link that points to your backend API
            var encodedUserId = Uri.EscapeDataString(user.Id);
            var encodedToken = Uri.EscapeDataString(token);
            var callbackUrl = $"{_configuration["ApiBaseUrl"]}/api/account/verify-email?userId={encodedUserId}&code={encodedToken}";

            _logger.LogInformation("Generated verification URL: {Url}", callbackUrl);

            // Send verification email
            await _emailService.SendEmailAsync(
                model.Email,
                "Confirm your email",
                $"Please confirm your account by <a href='{callbackUrl}'>clicking here</a>.");

            _logger.LogInformation("User {Email} registered successfully. Verification email sent.", model.Email);
            return Ok(new { message = "Registration successful. Please verify your email." });
        }


        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string userId, [FromQuery] string code)
        {
            _logger.LogInformation("Email verification attempt for userId: {UserId}", userId);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("Email verification failed: userId or code is null or empty");
                return Redirect($"{_configuration["ClientApp:BaseUrl"]}/verification-failed");
            }

            var decodedCode = Uri.UnescapeDataString(code); // Decode the token

            _logger.LogInformation("Decoded verification code: {Code}", decodedCode);

            // Find the user by ID
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Email verification failed: User {UserId} not found", userId);
                return Redirect($"{_configuration["ClientApp:BaseUrl"]}/verification-failed");
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedCode); // Use the decoded token
            if (result.Succeeded)
            {
                _logger.LogInformation("Email verification successful for user {UserId}", userId);
                return Redirect($"{_configuration["ClientApp:BaseUrl"]}/verification-success");
            }

            _logger.LogWarning("Email verification failed for user {UserId}: {Errors}",
                userId,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return Redirect($"{_configuration["ClientApp:BaseUrl"]}/verification-failed");
        }

        // In your Login endpoint:

        [HttpPost("login")]
        public async Task<IActionResult> Login(AuthModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // Check if email is verified
                if (!user.EmailConfirmed)
                {
                    _logger.LogWarning("Login attempt with unconfirmed email: {Email}", model.Email);
                    return BadRequest(new
                    {
                        message = "Please verify your email before logging in.",
                        requiresVerification = true
                    });
                }

                // Generate token (now correctly awaiting the async method)
                var token = await GenerateJwtToken(user);

                _logger.LogInformation("User logged in: {Email}", model.Email);
                return Ok(new { token = token, email = user.Email });
            }

            _logger.LogWarning("Login failed: {Email}", model.Email);
            return Unauthorized(new { message = "Invalid login attempt." });
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] AuthModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                return Ok(new { message = "If your account exists, a verification email has been sent." });
            }

            if (user.EmailConfirmed)
            {
                return Ok(new { message = "Your email is already verified. Please log in." });
            }

            // Generate and encode verification token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedUserId = Uri.EscapeDataString(user.Id);
            var encodedToken = Uri.EscapeDataString(token);
            var callbackUrl = $"{_configuration["ApiBaseUrl"]}/api/account/verify-email?userId={encodedUserId}&code={encodedToken}";

            await _emailService.SendEmailAsync(
                model.Email,
                "Confirm your email",
                $"Please confirm your account by <a href='{callbackUrl}'>clicking here</a>.");

            return Ok(new { message = "Verification email sent." });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("User logging out.");
            await _signInManager.SignOutAsync();
            return Ok("Logged out");
        }

        [HttpGet("me")]
        [Authorize] // This ensures only authenticated users can access this endpoint
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in token claims");
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return NotFound();
            }

            _logger.LogInformation("Returning user info for {Email}", user.Email);
            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                userName = user.UserName,
                emailConfirmed = user.EmailConfirmed
            });
        }

        // GET: api/account/is-admin
        [HttpGet("is-admin")]
        [Authorize]
        public async Task<IActionResult> CheckAdminStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            return Ok(new { isAdmin });
        }

        // POST: api/account/refresh
        [HttpPost("refresh")]
        [Authorize] // Requires an existing (possibly expired) token
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                // Get the user ID from the current token
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Token refresh failed: User ID not found in token");
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Find the user
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Token refresh failed: User {UserId} not found", userId);
                    return Unauthorized(new { message = "User not found" });
                }

                // Generate new token
                var newToken = await GenerateJwtToken(user);

                _logger.LogInformation("Token refreshed successfully for user {Email}", user.Email);
                return Ok(new { token = newToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { message = "An error occurred while refreshing the token" });
            }
        }

        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            // Add user ID to claims
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id)
    };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddHours(Convert.ToDouble(_configuration["Jwt:ExpireHours"]));

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
