namespace Game.Contracts
{
    public record ApiError(string Code, string Message, object? Details = null);
}
