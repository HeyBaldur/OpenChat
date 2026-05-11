using OpenChat.API.Models;

namespace OpenChat.API.Repositories;

public interface IChatRepository
{
    Task AddMessageAsync(ChatMessage message);
    Task<List<ChatMessage>> GetByConversationAsync(string conversationId, int limit = 30);
    Task DeleteByConversationAsync(string conversationId);
}
