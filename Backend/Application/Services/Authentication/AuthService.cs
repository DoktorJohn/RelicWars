using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Application.Services.Authentication
{
    public class AuthService : IAuthService
    {
        private const string LoginFailureMessage = "Invalid email or password, or the account is temporarily locked.";

        private readonly UserManager<PlayerProfile> _userManager;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly IJwtService _jwtService;

        public AuthService(
            UserManager<PlayerProfile> userManager,
            IPlayerProfileRepository playerProfileRepository,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _playerProfileRepository = playerProfileRepository;
            _jwtService = jwtService;
        }

        public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
        {
            string userName = request.UserName.Trim();
            string email = request.Email.Trim();
            string? userNameError = ValidateNewUserName(userName);
            if (userNameError != null)
            {
                return new AuthenticationResponse(false, userNameError, null, null);
            }

            if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
            {
                return new AuthenticationResponse(false, "Enter a valid email address.", null, null);
            }

            if (request.Password.Length is < 8 or > 128)
            {
                return new AuthenticationResponse(false, "Password must contain 8 to 128 characters.", null, null);
            }

            var newProfile = new PlayerProfile
            {
                UserName = userName,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            IdentityResult createResult;
            try
            {
                createResult = await _userManager.CreateAsync(newProfile, request.Password);
            }
            catch (DbUpdateException)
            {
                if (await _userManager.FindByNameAsync(userName) != null)
                {
                    return new AuthenticationResponse(false, "Username is already in use.", null, null);
                }

                if (await _userManager.FindByEmailAsync(email) != null)
                {
                    return new AuthenticationResponse(false, "Email is already in use.", null, null);
                }

                throw;
            }

            if (!createResult.Succeeded)
            {
                return new AuthenticationResponse(false, MapRegistrationErrors(createResult), null, null);
            }

            var token = _jwtService.GenerateToken(newProfile);
            var profileDto = new PlayerProfileDTO(newProfile.Id, userName, email, new List<WorldPlayerDTO>());

            return new AuthenticationResponse(true, "Profile created successfully.", token, profileDto);
        }

        public async Task<AuthenticationResponse> LoginAsync(LoginRequest request)
        {
            string email = request.Email.Trim();
            var profile = await _userManager.FindByEmailAsync(email);
            if (profile == null)
            {
                return new AuthenticationResponse(false, LoginFailureMessage, null, null);
            }

            if (await _userManager.IsLockedOutAsync(profile))
            {
                return new AuthenticationResponse(false, LoginFailureMessage, null, null);
            }

            if (!await _userManager.CheckPasswordAsync(profile, request.Password))
            {
                await _userManager.AccessFailedAsync(profile);
                return new AuthenticationResponse(false, LoginFailureMessage, null, null);
            }

            await _userManager.ResetAccessFailedCountAsync(profile);

            profile = await _playerProfileRepository.GetByIdAsync(profile.Id) ?? profile;
            var token = _jwtService.GenerateToken(profile);
            var worldDtos = profile.WorldPlayers
                .Select(worldPlayer => new WorldPlayerDTO(worldPlayer.Id, worldPlayer.WorldId))
                .ToList();

            var profileDto = new PlayerProfileDTO(
                profile.Id,
                profile.UserName ?? string.Empty,
                profile.Email ?? string.Empty,
                worldDtos);

            return new AuthenticationResponse(true, "Login successful.", token, profileDto);
        }

        private static string MapRegistrationErrors(IdentityResult result)
        {
            if (result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                return "Username is already in use.";
            }

            if (result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.DuplicateEmail)))
            {
                return "Email is already in use.";
            }

            return string.Join(" ", result.Errors.Select(error => error.Description));
        }

        private static string? ValidateNewUserName(string userName)
        {
            if (userName.Length is < 3 or > 20)
            {
                return "Username must contain 3 to 20 characters.";
            }

            foreach (char character in userName)
            {
                bool isAsciiLetter = character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
                bool isDigit = character is >= '0' and <= '9';
                if (!isAsciiLetter && !isDigit && character != '-' && character != '_')
                {
                    return "Username may only contain letters, numbers, hyphens, and underscores.";
                }
            }

            return null;
        }
    }
}
