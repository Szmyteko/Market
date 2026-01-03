namespace Market.ViewModels.Conversations;

public class ThreadListItemVm
{
    public Guid ConversationId { get; set; }
    public string OtherUserId { get; set; } = default!;
    public string OtherUserName { get; set; } = default!;
    public string? LastBody { get; set; }
    public DateTime? LastAtUtc { get; set; }
    public int UnreadCount { get; set; }
}