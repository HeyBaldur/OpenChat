using OpenChat.Domain.Entities;

namespace OpenChat.Domain.Interfaces.Repositories;

public interface ILogRepository
{
    Task AddAsync(Log log);
    Task<List<Log>> GetByConversationAsync(string conversationId);
}
