using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Market.Models;

public class Conversation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class ConversationMember
{
    [Required] public Guid ConversationId { get; set; }
    [Required] public string UserId { get; set; } = default!;
    public IdentityUser? User { get; set; }

    public DateTime LastReadUtc { get; set; } = DateTime.MinValue;

    [ForeignKey(nameof(ConversationId))] public Conversation Conversation { get; set; } = default!;
}

public class Message
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = default!;

    [Required, MaxLength(4000)] public string Body { get; set; } = default!;
    [Required] public string SenderId { get; set; } = default!;
    public IdentityUser? Sender { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}