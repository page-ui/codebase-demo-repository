using Page.Ui.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Page.Ui.Domain.Auth.Entities;
using Page.Ui.Domain.Chat.Entities;
using MassTransit;

namespace Page.Ui.Infrastructure.Auth.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PasswordResetCode> PasswordResetCodes { get; set; }
        public DbSet<PendingRegistration> PendingRegistrations { get; set; }

        public DbSet<Page.Ui.Domain.Chat.Entities.Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<AiRun> AiRuns { get; set; }
        public DbSet<AiRunFile> AiRunFiles { get; set; }
        public DbSet<RenderRun> RenderRuns { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.JwtId).IsRequired();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.HashedToken).IsUnique();
            });

            builder.Entity<PasswordResetCode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Code).IsRequired();
                entity.HasIndex(e => e.Email);
            });

            builder.Entity<PendingRegistration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.VerificationCode).IsRequired();
                entity.HasIndex(e => e.Email);
            });

            builder.Entity<Page.Ui.Domain.Chat.Entities.Chat>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OwnerUserId).IsRequired();
                entity.HasOne(e => e.OwnerUser)
                    .WithMany()
                    .HasForeignKey(e => e.OwnerUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.ChatKey).HasMaxLength(64).IsRequired();
                entity.HasIndex(e => e.ChatKey).IsUnique();
                entity.HasIndex(e => e.OwnerUserId);
                entity.HasIndex(e => new { e.OwnerUserId, e.CreatedAt });
                entity.HasIndex(e => new { e.OwnerUserId, e.UpdatedAt });
                entity.Property(e => e.ModelId).HasMaxLength(64).IsRequired();
                entity.Property(e => e.SystemPrompt).HasMaxLength(4000);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Chats_Name_MaxLength", "\"Name\" IS NULL OR char_length(\"Name\") <= 100");
                    t.HasCheckConstraint("CK_Chats_SystemPrompt_MaxLength", "\"SystemPrompt\" IS NULL OR char_length(\"SystemPrompt\") <= 4000");
                });
            });

            builder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Chat)
                      .WithMany(c => c.Messages)
                      .HasForeignKey(e => e.ChatId);
                entity.HasOne(e => e.Sender)
                      .WithMany()
                      .HasForeignKey(e => e.SenderId);

                entity.HasIndex(e => new { e.ChatId, e.CreatedAt });
                entity.HasIndex(e => new { e.ChatId, e.SenderId, e.ClientRequestId })
                    .IsUnique()
                    .HasFilter("\"ClientRequestId\" IS NOT NULL");
                entity.Property(e => e.Title).HasMaxLength(160).IsRequired();
                entity.Property(e => e.ClientRequestId).HasMaxLength(128);
                entity.Property(e => e.ServerGeneratedId).HasMaxLength(128);
                entity.Property(e => e.MessageKey).HasMaxLength(64).IsRequired();
                entity.HasIndex(e => e.MessageKey).IsUnique();
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Messages_Content_Length", "char_length(\"Content\") BETWEEN 1 AND 10000");
                    t.HasCheckConstraint("CK_Messages_Title_Length", "char_length(\"Title\") <= 160");
                });

                if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
                {
                    entity.Property<NpgsqlTsVector?>("SearchVector")
                        .HasColumnType("tsvector")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasAnnotation("Npgsql:TsVectorConfig", "english")
                        .HasAnnotation("Npgsql:TsVectorProperties", new[] { nameof(Message.Content) });
                    entity.HasIndex("SearchVector")
                        .HasMethod("GIN");
                }
            });

            builder.Entity<AiRun>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ChatId, e.IsCurrent });
                entity.HasIndex(e => new { e.ChatId, e.CreatedAt });
                entity.HasIndex(e => new { e.OwnerUserId, e.CreatedAt });
                entity.HasIndex(e => e.VersionId).IsUnique();
                entity.Property(e => e.OwnerUserId).HasMaxLength(450).IsRequired();
                entity.Property(e => e.ModelId).HasMaxLength(64).IsRequired();
                entity.Property(e => e.Title).HasMaxLength(160).IsRequired();
                entity.Property(e => e.ManifestObjectKey).HasMaxLength(1024).IsRequired();
                entity.Property(e => e.FinalPreviewUrl).HasMaxLength(2048);
                entity.Property(e => e.FailureCode).HasMaxLength(128);
                entity.Property(e => e.FailureMessageSafe).HasMaxLength(1000);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.HasOne(e => e.Chat)
                    .WithMany()
                    .HasForeignKey(e => e.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.TriggerMessage)
                    .WithMany()
                    .HasForeignKey(e => e.TriggerMessageId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.SupersededByRun)
                    .WithMany()
                    .HasForeignKey(e => e.SupersededByRunId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_AiRuns_Title_Length", "char_length(\"Title\") <= 160");
                });
            });

            builder.Entity<AiRunFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.RunId, e.Role });
                entity.Property(e => e.ObjectKey).HasMaxLength(1024).IsRequired();
                entity.Property(e => e.Role).HasMaxLength(64).IsRequired();
                entity.Property(e => e.OriginalFileName).HasMaxLength(260);
                entity.Property(e => e.StoredFileName).HasMaxLength(260).IsRequired();
                entity.Property(e => e.ContentType).HasMaxLength(256);
                entity.Property(e => e.Sha256).HasMaxLength(128);
                entity.HasOne(e => e.Run)
                    .WithMany(e => e.Files)
                    .HasForeignKey(e => e.RunId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<RenderRun>(entity =>
            {
                entity.HasKey(e => e.RunId);
                entity.Property(e => e.RunId).HasMaxLength(64);
                entity.Property(e => e.PublicRunToken).HasMaxLength(128).IsRequired();
                entity.Property(e => e.UserId).HasMaxLength(450);
                entity.Property(e => e.UserStorageKey).HasMaxLength(128);
                entity.Property(e => e.ChatKey).HasMaxLength(128);
                entity.Property(e => e.RelativeRunPath).HasMaxLength(1024).IsRequired();
                entity.Property(e => e.PreviewUrl).HasMaxLength(2048);
                entity.Property(e => e.ErrorSummary).HasMaxLength(1000);
                entity.Property(e => e.MetadataJson).HasColumnType("jsonb");
                entity.Property(e => e.ContentHash).HasMaxLength(128);
                entity.Property(e => e.SourceHash).HasMaxLength(128);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.HasIndex(e => new { e.UserId, e.CreatedAtUtc });
                entity.HasIndex(e => new { e.ChatId, e.CreatedAtUtc });
                entity.HasIndex(e => e.MessageId);
                entity.HasIndex(e => e.PublicRunToken).IsUnique();
                entity.HasIndex(e => new { e.Status, e.CreatedAtUtc });
            });

            builder.Entity<ApplicationUser>().HasData(new ApplicationUser
            {
                Id = Page.Ui.Domain.Chat.ChatConstants.AiUserId,
                UserName = "ai-system",
                NormalizedUserName = "AI-SYSTEM",
                Email = "ai@system.local",
                NormalizedEmail = "AI@SYSTEM.LOCAL",
                EmailConfirmed = true,
                Name = "AI Assistant",
                SecurityStamp = "AI_SYSTEM_SECURITY_STAMP"
            });

            builder.AddInboxStateEntity();
            builder.AddOutboxMessageEntity();
            builder.AddOutboxStateEntity();
        }
    }
}
