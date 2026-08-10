namespace Application.Exceptions;

public sealed class EdictConflictException : InvalidOperationException
{
    public EdictConflictException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}
