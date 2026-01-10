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
public class PlayerProfileDTO
{
    public string PlayerId;
    public string UserName;
    public string Email;
}

[Serializable]
public class GameWorldAvailableResponseDTO
{
    public string WorldId;
    public string WorldName;
    public int CurrentPlayerCount;
    public int MaxPlayerCapacity;
    public bool IsCurrentPlayerMember;
}

[Serializable]
public class PlayerWorldJoinResponse
{
    public bool ConnectionSuccessful;
    public string Message;
    public string ActiveCityId;
}