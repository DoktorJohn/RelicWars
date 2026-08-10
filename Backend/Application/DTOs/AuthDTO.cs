using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{

    //Used for authentication
    public record AuthenticationResponse(
        bool IsAuthenticated,
        string FeedbackMessage,
        string? JwtToken,
        PlayerProfileDTO? Profile
    );

    public record RegisterRequest(
        [Required, StringLength(20, MinimumLength = 3), RegularExpression("^[A-Za-z0-9_-]+$")]
        string UserName,
        [Required, EmailAddress, StringLength(256)]
        string Email,
        [Required, StringLength(128, MinimumLength = 8)]
        string Password);

    public record LoginRequest(
        [Required, EmailAddress, StringLength(256)]
        string Email,
        [Required]
        string Password);
}
