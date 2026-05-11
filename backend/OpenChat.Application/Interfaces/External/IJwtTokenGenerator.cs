using OpenChat.Domain.Entities;

namespace OpenChat.Application.Interfaces.External;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
