using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Services.Authentication;
using Game.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Handles requests to register new player profiles.
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthenticationResponse>> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);

            if (!result.IsAuthenticated)
            {
                _logger.LogWarning("Registration failed for email: {Email}. Reason: {Reason}", request.Email, result.FeedbackMessage);
                return BadRequest(new ApiError("auth.registration_failed", result.FeedbackMessage));
            }

            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            return Ok(result);
        }

        /// <summary>
        /// Validates credentials and returns JWT token + OwnerWorldPlayer references.
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (!result.IsAuthenticated)
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                return Unauthorized(new ApiError("auth.login_failed", result.FeedbackMessage));
            }

            return Ok(result);
        }
    }
}
