using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{

    //Used for authentication
    public record AuthenticationResponse(
        bool IsAuthenticated,
        string FeedbackMessage,
        string? JwtToken,
        PlayerProfileDTO? Profile
    );

    public record RegisterRequest(string UserName, string Email, string Password);
    public record LoginRequest(string Email, string Password);
}
