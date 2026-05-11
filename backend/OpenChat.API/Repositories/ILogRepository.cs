using OpenChat.API.Models;

namespace OpenChat.API.Repositories;

public interface ILogRepository
{
    Task AddAsync(Log log);
    Task<List<Log>> GetByConversationAsync(string conversationId);
}
