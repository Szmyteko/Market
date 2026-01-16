using Market.Data;
using Market.Models;
using Market.ViewModels.Conversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Market.Controllers;

[Authorize]
public class ConversationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _um;
    public ConversationsController(ApplicationDbContext db, UserManager<IdentityUser> um) { _db = db; _um = um; }


    public async Task<IActionResult> Index()
    {
        var me = _um.GetUserId(User)!;

        var mine = await _db.ConversationMembers
            .Where(m => m.UserId == me)
            .Select(m => new ThreadListItemVm
            {
                ConversationId = m.ConversationId,
                OtherUserId = _db.ConversationMembers
                                  .Where(x => x.ConversationId == m.ConversationId && x.UserId != me)
                                  .Select(x => x.UserId).FirstOrDefault()!,
                OtherUserName = _db.Users
                                  .Where(u => u.Id == _db.ConversationMembers
                                      .Where(x => x.ConversationId == m.ConversationId && x.UserId != me)
                                      .Select(x => x.UserId).FirstOrDefault())
                                  .Select(u => u.UserName ?? u.Email).FirstOrDefault()!,
                LastBody = _db.Messages.Where(msg => msg.ConversationId == m.ConversationId)
                                       .OrderByDescending(msg => msg.CreatedUtc)
                                       .Select(msg => msg.Body).FirstOrDefault(),
                LastAtUtc = _db.Messages.Where(msg => msg.ConversationId == m.ConversationId)
                                        .Max(msg => (DateTime?)msg.CreatedUtc),
                UnreadCount = _db.Messages.Where(msg => msg.ConversationId == m.ConversationId
                                                     && msg.CreatedUtc > m.LastReadUtc
                                                     && msg.SenderId != me).Count()
            })
            .OrderByDescending(x => x.LastAtUtc)
            .ToListAsync();

        return View(mine);
    }

  
    [HttpPost]
    public async Task<IActionResult> Start(string toUserId)
    {
        var me = _um.GetUserId(User)!;
        if (string.IsNullOrWhiteSpace(toUserId) || toUserId == me) return RedirectToAction(nameof(Index));

        
        var myConvIds = await _db.ConversationMembers.Where(m => m.UserId == me).Select(m => m.ConversationId).ToListAsync();
        var convId = await _db.ConversationMembers
            .Where(m => m.UserId == toUserId && myConvIds.Contains(m.ConversationId))
            .Select(m => (Guid?)m.ConversationId)
            .FirstOrDefaultAsync();

        Conversation conv;
        if (convId is null)
        {
            conv = new Conversation();
            _db.Conversations.Add(conv);
            _db.ConversationMembers.AddRange(
                new ConversationMember { ConversationId = conv.Id, UserId = me },
                new ConversationMember { ConversationId = conv.Id, UserId = toUserId }
            );
            await _db.SaveChangesAsync();
        }
        else
        {
            conv = await _db.Conversations.FindAsync(convId.Value) ?? throw new InvalidOperationException();
        }

        return RedirectToAction(nameof(Thread), new { id = conv.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Thread(Guid id)
    {
        var me = _um.GetUserId(User)!;

        var member = await _db.ConversationMembers.FirstOrDefaultAsync(m => m.ConversationId == id && m.UserId == me);
        if (member is null) return Forbid();

        var otherId = await _db.ConversationMembers.Where(m => m.ConversationId == id && m.UserId != me)
                         .Select(m => m.UserId).FirstOrDefaultAsync();
        var otherName = await _db.Users.Where(u => u.Id == otherId).Select(u => u.UserName ?? u.Email).FirstOrDefaultAsync() ?? otherId ?? "użytkownik";

        var msgs = await _db.Messages.Where(x => x.ConversationId == id)
            .OrderBy(x => x.CreatedUtc)
            .Include(x => x.Sender)
            .ToListAsync();

        member.LastReadUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var vm = new ThreadVm { ConversationId = id, OtherUserId = otherId!, OtherUserName = otherName, Messages = msgs };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(Guid conversationId, string body)
    {
        var me = _um.GetUserId(User)!;
        if (string.IsNullOrWhiteSpace(body)) return RedirectToAction(nameof(Thread), new { id = conversationId });

        var isMember = await _db.ConversationMembers.AnyAsync(m => m.ConversationId == conversationId && m.UserId == me);
        if (!isMember) return Forbid();

        _db.Messages.Add(new Message { ConversationId = conversationId, SenderId = me, Body = body.Trim(), CreatedUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var my = await _db.ConversationMembers.FirstAsync(m => m.ConversationId == conversationId && m.UserId == me);
        my.LastReadUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Thread), new { id = conversationId });
    }
}
