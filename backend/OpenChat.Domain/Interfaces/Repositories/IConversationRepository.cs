using OpenChat.Domain.Entities;

namespace OpenChat.Domain.Interfaces.Repositories;

public interface IConversationRepository
{
    Task<Conversation> CreateAsync(string userId, string title, string? model = null);
    Task<List<Conversation>> GetByUserAsync(string userId);
    Task<Conversation?> GetByIdAsync(string conversationId);
    Task UpdateTitleAsync(string conversationId, string title);
    Task AddTokensAsync(string conversationId, int tokens);
    Task UpdateModelAsync(string conversationId, string model);
    Task DeleteAsync(string conversationId);
}
