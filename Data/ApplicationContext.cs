using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReemRPG.Models;

namespace ReemRPG.Data
{
    public class ApplicationContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

        // Tables in the database
        public DbSet<Character> Characters { get; set; }
        public DbSet<UserCharacter> UserCharacters { get; set; }
        public DbSet<Quest> Quests { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<CharacterQuest> CharacterQuests { get; set; }
        public DbSet<QuestCompletion> QuestCompletions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Usercharacter entity
            modelBuilder.Entity<UserCharacter>(entity =>
            {
                // Set composite primary key
                entity.HasKey(e => new { e.UserId, e.CharacterId });

                // Configure Character relationship
                entity.HasOne(e => e.Character)
                    .WithMany()
                    .HasForeignKey(e => e.CharacterId)
                    .OnDelete(DeleteBehavior.Restrict);  // Prevent cascade delete

                // Configure IdentityUser relationship
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);   // Allow cascade delete for users
            });

            // Define composite key for Inventory (CharacterId + ItemId)
            modelBuilder.Entity<Inventory>()
                .HasKey(i => new { i.CharacterId, i.ItemId });

            // Define composite key for CharacterQuest (CharacterId + QuestId)
            modelBuilder.Entity<CharacterQuest>()
                .HasKey(cq => new { cq.CharacterId, cq.QuestId });

            // Configure CharacterQuest relationships
            modelBuilder.Entity<CharacterQuest>()
                .HasOne(cq => cq.Character)
                .WithMany(c => c.CharacterQuests)
                .HasForeignKey(cq => cq.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CharacterQuest>()
                .HasOne(cq => cq.Quest)
                .WithMany(q => q.CharacterQuests)
                .HasForeignKey(cq => cq.QuestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestCompletion>()
                .HasOne(qc => qc.Character)
                .WithMany()
                .HasForeignKey(qc => qc.CharacterId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}