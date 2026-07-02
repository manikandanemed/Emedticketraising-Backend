using Microsoft.EntityFrameworkCore;
using TeamTrack.Models;

namespace TeamTrack.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<WorkItem> WorkItems => Set<WorkItem>();
        public DbSet<Bug> Bugs => Set<Bug>();
        public DbSet<DailyStatusNote> DailyStatusNotes => Set<DailyStatusNote>();
        public DbSet<PersonalNote> PersonalNotes => Set<PersonalNote>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Module> Modules => Set<Module>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).HasColumnName("id");
                entity.Property(u => u.Name).HasColumnName("name");
                entity.Property(u => u.Email).HasColumnName("email");
                entity.Property(u => u.Mobile).HasColumnName("mobile");
                entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
                entity.Property(u => u.UserType).HasColumnName("user_type");
                entity.Property(u => u.IsActive).HasColumnName("is_active");
                entity.Property(u => u.CreatedAt).HasColumnName("created_at");
                entity.Property(u => u.ProfilePicture).HasColumnName("profile_picture");
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.ToTable("tickets");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Id).HasColumnName("id");
                entity.Property(t => t.TicketNumber).HasColumnName("ticket_number");
                entity.Property(t => t.Title).HasColumnName("title");
                entity.Property(t => t.Description).HasColumnName("description");
                entity.Property(t => t.Category).HasColumnName("category");
                entity.Property(t => t.Priority).HasColumnName("priority");
                entity.Property(t => t.Status).HasColumnName("status");
                entity.Property(t => t.RaisedByUserId).HasColumnName("raised_by_user_id");
                entity.Property(t => t.AssignedToUserId).HasColumnName("assigned_to_user_id");
                entity.Property(t => t.WhatsappNotify).HasColumnName("whatsapp_notify");
                entity.Property(t => t.CreatedAt).HasColumnName("created_at");
                entity.Property(t => t.UpdatedAt).HasColumnName("updated_at");
                entity.Property(t => t.ResolvedAt).HasColumnName("resolved_at");
                entity.Property(t => t.ClosedAt).HasColumnName("closed_at");
                entity.HasIndex(t => t.TicketNumber).IsUnique();

                entity.HasOne(t => t.RaisedBy)
                      .WithMany(u => u.RaisedTickets)
                      .HasForeignKey(t => t.RaisedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.AssignedTo)
                      .WithMany(u => u.AssignedTickets)
                      .HasForeignKey(t => t.AssignedToUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.ToTable("comments");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).HasColumnName("id");
                entity.Property(c => c.WorkItemId).HasColumnName("work_item_id");
                entity.Property(c => c.UserId).HasColumnName("user_id");
                entity.Property(c => c.Message).HasColumnName("message");
                entity.Property(c => c.IsInternal).HasColumnName("is_internal");
                entity.Property(c => c.CreatedAt).HasColumnName("created_at");

                entity.HasOne(c => c.WorkItem)
                      .WithMany()
                      .HasForeignKey(c => c.WorkItemId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                      .WithMany(u => u.Comments)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("projects");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).HasColumnName("id");
                entity.Property(p => p.ProjectNumber).HasColumnName("project_number");
                entity.Property(p => p.Name).HasColumnName("name");
                entity.Property(p => p.Description).HasColumnName("description");
                entity.Property(p => p.Status).HasColumnName("status");
                entity.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
                entity.Property(p => p.ClientId).HasColumnName("client_id");
                entity.Property(p => p.CreatedAt).HasColumnName("created_at");
                entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");
                entity.HasIndex(p => p.ProjectNumber).IsUnique();

                entity.HasOne(p => p.Client)
                      .WithMany(c => c.Projects)
                      .HasForeignKey(p => p.ClientId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.CreatedBy)
                      .WithMany()
                      .HasForeignKey(p => p.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(p => p.AssignedEmployees)
                      .WithMany(u => u.AssignedProjects)
                      .UsingEntity<Dictionary<string, object>>(
                          "project_members",
                          j => j.HasOne<User>().WithMany().HasForeignKey("user_id"),
                          j => j.HasOne<Project>().WithMany().HasForeignKey("project_id")
                      );
            });

            modelBuilder.Entity<WorkItem>(entity =>
            {
                entity.ToTable("work_items");
                entity.HasKey(w => w.Id);
                entity.Property(w => w.Id).HasColumnName("id");
                entity.Property(w => w.WorkNumber).HasColumnName("work_number");
                entity.Property(w => w.Title).HasColumnName("title");
                entity.Property(w => w.Description).HasColumnName("description");
                entity.Property(w => w.Status).HasColumnName("status");
                entity.Property(w => w.Priority).HasColumnName("priority");
                entity.Property(w => w.ProjectId).HasColumnName("project_id");
                entity.Property(w => w.ModuleId).HasColumnName("module_id");
                entity.Property(w => w.CreatedByUserId).HasColumnName("created_by_user_id");
                entity.Property(w => w.AssignedToUserId).HasColumnName("assigned_to_user_id");
                entity.Property(w => w.CreatedAt).HasColumnName("created_at");
                entity.Property(w => w.UpdatedAt).HasColumnName("updated_at");
                entity.Property(w => w.CompletedAt).HasColumnName("completed_at");
                entity.Property(w => w.DueDate).HasColumnName("due_date");
                entity.Property(w => w.WorkType).HasColumnName("work_type");
                entity.Property(w => w.StartDate).HasColumnName("start_date");
                entity.Property(w => w.ParentId).HasColumnName("parent_id");
                entity.Property(w => w.Labels).HasColumnName("labels");
                entity.Property(w => w.Team).HasColumnName("team");
                entity.Property(w => w.AttachmentUrls).HasColumnName("attachment_urls");
                entity.HasIndex(w => w.WorkNumber).IsUnique();

                entity.HasOne(w => w.Project)
                      .WithMany(p => p.WorkItems)
                      .HasForeignKey(w => w.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(w => w.Module)
                      .WithMany(m => m.WorkItems)
                      .HasForeignKey(w => w.ModuleId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(w => w.CreatedBy)
                      .WithMany()
                      .HasForeignKey(w => w.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(w => w.AssignedTo)
                      .WithMany()
                      .HasForeignKey(w => w.AssignedToUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });


            modelBuilder.Entity<Bug>(entity =>
            {
                entity.ToTable("bugs");
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Id).HasColumnName("id");
                entity.Property(b => b.BugNumber).HasColumnName("bug_number");
                entity.Property(b => b.Title).HasColumnName("title");
                entity.Property(b => b.Description).HasColumnName("description");
                entity.Property(b => b.ScreenshotUrl).HasColumnName("screenshot_url");
                entity.Property(b => b.Status).HasColumnName("status");
                entity.Property(b => b.WorkItemId).HasColumnName("work_item_id");
                entity.Property(b => b.RaisedByUserId).HasColumnName("raised_by_user_id");
                entity.Property(b => b.AssignedToUserId).HasColumnName("assigned_to_user_id");
                entity.Property(b => b.CreatedAt).HasColumnName("created_at");
                entity.Property(b => b.UpdatedAt).HasColumnName("updated_at");
                entity.Property(b => b.FixedAt).HasColumnName("fixed_at");
                entity.Property(b => b.ClosedAt).HasColumnName("closed_at");
                entity.HasIndex(b => b.BugNumber).IsUnique();

                entity.HasOne(b => b.WorkItem)
                      .WithMany()
                      .HasForeignKey(b => b.WorkItemId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.RaisedBy)
                      .WithMany()
                      .HasForeignKey(b => b.RaisedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.AssignedTo)
                      .WithMany()
                      .HasForeignKey(b => b.AssignedToUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<DailyStatusNote>(entity =>
            {
                entity.ToTable("daily_status_notes");
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Id).HasColumnName("id");
                entity.Property(n => n.EmployeeId).HasColumnName("employee_id");
                entity.Property(n => n.CreatedByUserId).HasColumnName("created_by_user_id");
                entity.Property(n => n.NoteText).HasColumnName("note_text");
                entity.Property(n => n.CreatedAt).HasColumnName("created_at");

                entity.HasOne(n => n.Employee)
                      .WithMany()
                      .HasForeignKey(n => n.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.CreatedBy)
                      .WithMany()
                      .HasForeignKey(n => n.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PersonalNote>(entity =>
            {
                entity.ToTable("personal_notes");
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Id).HasColumnName("id");
                entity.Property(n => n.UserId).HasColumnName("user_id");
                entity.Property(n => n.Content).HasColumnName("content");
                entity.Property(n => n.CreatedAt).HasColumnName("created_at");
                entity.Property(n => n.NoteDate).HasColumnName("note_date");
                entity.Property(n => n.Priority).HasColumnName("priority");

                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("clients");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).HasColumnName("id");
                entity.Property(c => c.ClientNumber).HasColumnName("client_number");
                entity.Property(c => c.Name).HasColumnName("name");
                entity.Property(c => c.Description).HasColumnName("description");
                entity.Property(c => c.CreatedAt).HasColumnName("created_at");
                entity.Property(c => c.UpdatedAt).HasColumnName("updated_at");
                entity.HasIndex(c => c.ClientNumber).IsUnique();
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("products");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).HasColumnName("id");
                entity.Property(p => p.ProductNumber).HasColumnName("product_number");
                entity.Property(p => p.Name).HasColumnName("name");
                entity.Property(p => p.Description).HasColumnName("description");
                entity.Property(p => p.ProjectId).HasColumnName("project_id");
                entity.Property(p => p.CreatedAt).HasColumnName("created_at");
                entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");
                entity.HasIndex(p => p.ProductNumber).IsUnique();

                entity.HasOne(p => p.Project)
                      .WithMany()
                      .HasForeignKey(p => p.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Module>(entity =>
            {
                entity.ToTable("modules");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Id).HasColumnName("id");
                entity.Property(m => m.ModuleNumber).HasColumnName("module_number");
                entity.Property(m => m.Name).HasColumnName("name");
                entity.Property(m => m.Description).HasColumnName("description");
                entity.Property(m => m.ProductId).HasColumnName("product_id");
                entity.Property(m => m.CreatedAt).HasColumnName("created_at");
                entity.Property(m => m.UpdatedAt).HasColumnName("updated_at");
                entity.HasIndex(m => m.ModuleNumber).IsUnique();

                entity.HasOne(m => m.Product)
                      .WithMany(p => p.Modules)
                      .HasForeignKey(m => m.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
