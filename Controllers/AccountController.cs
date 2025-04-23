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

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} registered successfully.", model.Email);

                // Generate an email verification token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Create the verification link
                var verificationLink = Url.Action("VerifyEmail", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                // Send the verification email
                var emailSubject = "Email Verification";
                var emailBody = $"Please verify your email by clicking the following link: {verificationLink}";

                await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
                _logger.LogInformation("Verification email sent to {Email}.", user.Email);

                return Ok("User registered successfully. An email verification link has been sent.");
            }

            _logger.LogWarning("Registration failed for {Email}. Errors: {Errors}", model.Email, result.Errors);
            return BadRequest(result.Errors);
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail(string userId, string token)
        {
            _logger.LogInformation("Attempting email verification for userId: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Email verification failed. User {UserId} not found.", userId);
                return NotFound("User not found.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                _logger.LogInformation("Email verification successful for {UserId}.", userId);
                return Ok("Email verification successful.");
            }

            _logger.LogWarning("Email verification failed for {UserId}.", userId);
            return BadRequest("Email verification failed.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(AuthModel model)
        {
            _logger.LogInformation("User attempting login: {Email}", model.Email);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                var roles = await _userManager.GetRolesAsync(user);
                var token = GenerateJwtToken(user, roles);

                _logger.LogInformation("User {Email} logged in successfully.", model.Email);
                return Ok(new { Token = token });
            }

            _logger.LogWarning("Invalid login attempt for {Email}.", model.Email);
            return Unauthorized("Invalid login attempt.");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("User logging out.");
            await _signInManager.SignOutAsync();
            return Ok("Logged out");
        }

        private string GenerateJwtToken(IdentityUser user, IList<string> roles)
        {
            _logger.LogInformation("Generating JWT token for user {Email}", user.Email);
            _logger.LogInformation("Generating JWT token for user {Id}", user.Id);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

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

            _logger.LogInformation("JWT token generated for {Email}", user.Email);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
