using Application.DTOs;
using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _playerAuthenticationService;

        public AuthController(IAuthService playerAuthenticationService)
        {
            _playerAuthenticationService = playerAuthenticationService;
        }

        /// <summary>
        /// Behandler anmodninger om oprettelse af nye globale spillerprofiler.
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthenticationResponse>> ProcessPlayerRegistrationRequest([FromBody] RegisterRequest registrationRequest)
        {
            var registrationResult = await _playerAuthenticationService.RegisterAsync(registrationRequest);

            // RETTELSE: Vi tjekker nu på 'IsAuthenticated' i stedet for 'Success'
            if (!registrationResult.IsAuthenticated)
            {
                return BadRequest(registrationResult);
            }

            return Ok(registrationResult);
        }

        /// <summary>
        /// Validerer spillerens legitimationsoplysninger og udsteder et JWT token ved succes.
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationResponse>> ProcessPlayerLoginRequest([FromBody] LoginRequest loginRequest)
        {
            var loginResult = await _playerAuthenticationService.LoginAsync(loginRequest);

            // RETTELSE: Vi tjekker nu på 'IsAuthenticated' i stedet for 'Success'
            if (!loginResult.IsAuthenticated)
            {
                // Vi returnerer Unauthorized (401) ved forkert login
                return Unauthorized(loginResult);
            }

            return Ok(loginResult);
        }
    }
}