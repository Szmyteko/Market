using Market.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Market.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // --- Core ---
    public DbSet<Property> Property { get; set; } = default!;
    public DbSet<PropertyImage> PropertyImage { get; set; } = default!;
    public DbSet<RentalRequest> RentalRequest { get; set; } = default!;
    public DbSet<RentalAgreement> RentalAgreement { get; set; } = default!;
    public DbSet<Payment> Payment { get; set; } = default!;
    public DbSet<MaintenanceRequest> MaintenanceRequest { get; set; } = default!;

    // --- Admin/Views (zachowane jak w projekcie) ---
    public DbSet<UsersViewModel> UsersViewModel { get; set; } = default!;

    // --- Weryfikacje ---
    public DbSet<VerificationRequest> VerificationRequests => Set<VerificationRequest>();
    public DbSet<UserVerification> UserVerification => Set<UserVerification>();

    // --- Wiadomości 1:1 ---
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== WERYFIKACJE =====
        modelBuilder.Entity<VerificationRequest>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserVerification>()
            .HasKey(x => x.UserId);

        // ===== PROPERTY / PAYMENT / MAINTENANCE =====
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Property)
            .WithMany(pr => pr.Payments)
            .HasForeignKey(p => p.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaintenanceRequest>()
            .HasOne(m => m.Property)
            .WithMany(p => p.ServiceRequests!)
            .HasForeignKey(m => m.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Moderacja ogłoszeń
        modelBuilder.Entity<Property>()
            .HasOne(p => p.ApprovedByUser)
            .WithMany()
            .HasForeignKey(p => p.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Property>()
            .HasIndex(p => new { p.ApprovalStatus, p.IsDeleted });

        // Zdjęcia ogłoszeń
        modelBuilder.Entity<PropertyImage>()
            .HasOne(i => i.Property)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PropertyImage>()
            .HasIndex(i => new { i.PropertyId, i.SortOrder });

        // ===== RENTAL REQUEST =====
        modelBuilder.Entity<RentalRequest>()
            .HasOne(rr => rr.Property)
            .WithMany() // jeśli masz kolekcję w Property, zamień na .WithMany(p => p.RentalRequests)
            .HasForeignKey(rr => rr.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RentalRequest>()
            .HasOne(rr => rr.Requester)
            .WithMany()
            .HasForeignKey(rr => rr.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RentalRequest>()
            .Property(r => r.StartDate).HasColumnType("date");
        modelBuilder.Entity<RentalRequest>()
            .Property(r => r.EndDate).HasColumnType("date");

        modelBuilder.Entity<RentalRequest>()
            .HasIndex(r => new { r.PropertyId, r.StartDate, r.EndDate });

        modelBuilder.Entity<RentalRequest>()
            .HasIndex(r => new { r.PropertyId, r.Status });

        // Unikalne "Pending" per (PropertyId, RequesterId) – SQL Server filter
        modelBuilder.Entity<RentalRequest>()
            .HasIndex(r => new { r.PropertyId, r.RequesterId })
            .IsUnique()
            .HasDatabaseName("UX_RentalRequest_Pending_PerUser")
            .HasFilter("[Status] = 0"); // Pending

        // ===== RENTAL AGREEMENT =====
        modelBuilder.Entity<RentalAgreement>()
            .HasOne(ra => ra.Property)
            .WithMany(p => p.RentalAgreements)
            .HasForeignKey(ra => ra.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RentalAgreement>()
            .HasOne(ra => ra.Tenant)
            .WithMany()
            .HasForeignKey(ra => ra.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RentalAgreement>()
            .HasOne(ra => ra.User)
            .WithMany()
            .HasForeignKey(ra => ra.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RentalAgreement>()
            .Property(a => a.StartDate).HasColumnType("date");
        modelBuilder.Entity<RentalAgreement>()
            .Property(a => a.EndDate).HasColumnType("date");

        modelBuilder.Entity<RentalAgreement>()
            .HasIndex(a => new { a.PropertyId, a.StartDate, a.EndDate });

        // ===== PAYMENT =====
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.RentalAgreement)
            .WithMany()
            .HasForeignKey(p => p.RentalAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasIndex(p => new { p.RentalAgreementId, p.Status });

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.Reference);

        // DECIMAL precision/scale
        modelBuilder.Entity<Property>()
            .Property(p => p.RentPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<RentalAgreement>()
            .Property(a => a.MonthlyRent)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        // ===== MESSAGING =====
        modelBuilder.Entity<ConversationMember>().HasKey(x => new { x.ConversationId, x.UserId });

        modelBuilder.Entity<ConversationMember>()
            .HasOne(x => x.Conversation)
            .WithMany(c => c.Members)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConversationMember>()
            .HasOne<IdentityUser>(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasOne<IdentityUser>(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.ConversationId, m.CreatedUtc });
    }
}
