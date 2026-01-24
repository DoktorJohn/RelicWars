using Project.Scripts.Domain.Enums;
using System;

[Serializable]
public class AuthenticationResponse
{
    public bool IsAuthenticated;
    public string JwtToken;
    public string FeedbackMessage;
    public PlayerProfileDTO Profile;
}

[Serializable]
public class PlayerWorldJoinResponse
{
    public bool ConnectionSuccessful;
    public string Message;
    public string ActiveCityId;
    public string WorldPlayerId;
    public IdeologyTypeEnum SelectedIdeology;
}