namespace MyApp.Application;

public sealed class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException(string message, Exception? inner = null) : base(message, inner) { }
}
