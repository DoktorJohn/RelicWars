using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Authentication
{
    public class AuthService : IAuthService
    {
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly IJwtService _jwtService;

        public AuthService(IPlayerProfileRepository playerProfileRepository, IJwtService jwtService)
        {
            _playerProfileRepository = playerProfileRepository;
            _jwtService = jwtService;
        }

        public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _playerProfileRepository.ExistsByEmailAsync(request.Email))
                return new AuthenticationResponse(false, "Email is already in use.", null, null);

            string passwordSalt = BCrypt.Net.BCrypt.GenerateSalt(11);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, passwordSalt);

            var newProfile = new PlayerProfile
            {
                UserName = request.UserName,
                Email = request.Email,
                PasswordHash = hashedPassword
            };

            await _playerProfileRepository.AddAsync(newProfile);

            var token = _jwtService.GenerateToken(newProfile);

            var profileDto = new PlayerProfileDTO(newProfile.Id, newProfile.UserName, newProfile.Email, new List<WorldPlayerDTO>());

            return new AuthenticationResponse(true, "Profile created successfully.", token, profileDto);
        }

        public async Task<AuthenticationResponse> LoginAsync(LoginRequest request)
        {
            var profile = await _playerProfileRepository.GetByEmailAsync(request.Email);

            if (profile == null || !BCrypt.Net.BCrypt.Verify(request.Password, profile.PasswordHash))
            {
                return new AuthenticationResponse(false, "Invalid email or password.", null, null);
            }

            var token = _jwtService.GenerateToken(profile);

            // MAP DATA: Convert the Entity WorldPlayers to DTOs so the client receives them
            var worldDtos = profile.WorldPlayers
                .Select(wp => new WorldPlayerDTO(wp.Id, wp.WorldId))
                .ToList();

            var profileDto = new PlayerProfileDTO(
                profile.Id,
                profile.UserName ?? string.Empty,
                profile.Email ?? string.Empty,
                worldDtos
            );

            return new AuthenticationResponse(true, "Login successful.", token, profileDto);
        }
    }
}