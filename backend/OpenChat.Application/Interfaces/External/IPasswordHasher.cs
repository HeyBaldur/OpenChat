namespace OpenChat.Application.Interfaces.External;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
