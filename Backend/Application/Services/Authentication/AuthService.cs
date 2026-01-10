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

        public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest registrationRequest)
        {
            var existingProfile = await _playerProfileRepository.GetByEmailAsync(registrationRequest.Email);
            if (existingProfile != null)
                return new AuthenticationResponse(false, "Emailen er allerede i brug.", null, null);

            string passwordSalt = BCrypt.Net.BCrypt.GenerateSalt(12);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registrationRequest.Password, passwordSalt);

            var newPlayerProfile = new PlayerProfile
            {
                UserName = registrationRequest.UserName,
                Email = registrationRequest.Email,
                PasswordHash = hashedPassword
            };

            await _playerProfileRepository.AddAsync(newPlayerProfile);

            var authToken = _jwtService.GenerateToken(newPlayerProfile);
            var profileData = MapToProfileDto(newPlayerProfile);

            return new AuthenticationResponse(true, "Brugerprofil oprettet korrekt.", authToken, profileData);
        }

        public async Task<AuthenticationResponse> LoginAsync(LoginRequest loginRequest)
        {
            var existingProfile = await _playerProfileRepository.GetByEmailAsync(loginRequest.Email);

            if (existingProfile == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, existingProfile.PasswordHash))
                return new AuthenticationResponse(false, "Ugyldig email eller adgangskode.", null, null);

            var authToken = _jwtService.GenerateToken(existingProfile);
            var profileData = MapToProfileDto(existingProfile);

            return new AuthenticationResponse(true, "Login gennemført.", authToken, profileData);
        }

        private PlayerProfileDTO MapToProfileDto(PlayerProfile profile)
        {
            return new PlayerProfileDTO(profile.Id, profile.UserName, profile.Email);
        }
    }
}