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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserCharacter>()
           .HasKey(uc => new { uc.UserId, uc.CharacterId }); // Composite primary key

            modelBuilder.Entity<UserCharacter>()
                .HasOne(uc => uc.Character)
                .WithMany()
                .HasForeignKey(uc => uc.CharacterId);


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
        }
    }
}