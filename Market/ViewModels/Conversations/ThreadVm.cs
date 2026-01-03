using Market.Models;

namespace Market.ViewModels.Conversations;

public class ThreadVm
{
    public Guid ConversationId { get; set; }
    public string OtherUserId { get; set; } = default!;
    public string OtherUserName { get; set; } = default!;
    public IReadOnlyList<Message> Messages { get; set; } = Array.Empty<Message>();
}