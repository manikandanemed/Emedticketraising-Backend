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
        public DbSet<SoftwareBuild> SoftwareBuilds => Set<SoftwareBuild>();
        public DbSet<WorkItemActivityLog> WorkItemActivityLogs => Set<WorkItemActivityLog>();
        public DbSet<UserOtp> UserOtps => Set<UserOtp>();

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
                entity.Property(t => t.ProjectId).HasColumnName("project_id");
                entity.Property(t => t.RaisedByUserId).HasColumnName("raised_by_user_id");
                entity.Property(t => t.AssignedToUserId).HasColumnName("assigned_to_user_id");
                entity.Property(t => t.BuildId).HasColumnName("build_id");
                entity.Property(t => t.WhatsappNotify).HasColumnName("whatsapp_notify");
                entity.Property(t => t.CreatedAt).HasColumnName("created_at");
                entity.Property(t => t.UpdatedAt).HasColumnName("updated_at");
                entity.Property(t => t.ResolvedAt).HasColumnName("resolved_at");
                entity.Property(t => t.ClosedAt).HasColumnName("closed_at");
                entity.HasIndex(t => t.TicketNumber).IsUnique();

                entity.HasOne(t => t.Project)
                      .WithMany()
                      .HasForeignKey(t => t.ProjectId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.RaisedBy)
                      .WithMany(u => u.RaisedTickets)
                      .HasForeignKey(t => t.RaisedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.AssignedTo)
                      .WithMany(u => u.AssignedTickets)
                      .HasForeignKey(t => t.AssignedToUserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.Build)
                      .WithMany()
                      .HasForeignKey(t => t.BuildId)
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
                entity.Property(w => w.EpicName).HasColumnName("epic_name");
                entity.Property(w => w.EpicColor).HasColumnName("epic_color");
                entity.Property(w => w.FixedBillNumber).HasColumnName("fixed_bill_number");
                entity.Property(w => w.RaisedBillNumber).HasColumnName("raised_bill_number");
                entity.Property(w => w.DeveloperBillLock).HasColumnName("developer_bill_lock");
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
                entity.Property(b => b.RaisedBuild).HasColumnName("raised_build");
                entity.Property(b => b.FixedBuild).HasColumnName("fixed_build");
                entity.Property(b => b.Severity).HasColumnName("severity");
                entity.Property(b => b.IssueType).HasColumnName("issue_type");
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
                entity.Property(n => n.AssignedToUserId).HasColumnName("assigned_to_user_id");

                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.AssignedTo)
                      .WithMany()
                      .HasForeignKey(n => n.AssignedToUserId)
                      .OnDelete(DeleteBehavior.SetNull);
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

            modelBuilder.Entity<SoftwareBuild>(entity =>
            {
                entity.ToTable("software_builds");
                entity.HasKey(sb => sb.Id);
                entity.Property(sb => sb.Id).HasColumnName("id");
                entity.Property(sb => sb.BuildNumber).HasColumnName("build_number");
                entity.Property(sb => sb.ProjectId).HasColumnName("project_id");
                entity.Property(sb => sb.IsActive).HasColumnName("is_active");
                entity.Property(sb => sb.CreatedAt).HasColumnName("created_at");

                entity.HasOne(sb => sb.Project)
                      .WithMany()
                      .HasForeignKey(sb => sb.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WorkItemActivityLog>(entity =>
            {
                entity.ToTable("work_item_activity_logs");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Id).HasColumnName("id");
                entity.Property(a => a.WorkItemId).HasColumnName("work_item_id");
                entity.Property(a => a.Action).HasColumnName("action");
                entity.Property(a => a.FromUserId).HasColumnName("from_user_id");
                entity.Property(a => a.ToUserId).HasColumnName("to_user_id");
                entity.Property(a => a.FromStatus).HasColumnName("from_status");
                entity.Property(a => a.ToStatus).HasColumnName("to_status");
                entity.Property(a => a.ByUserId).HasColumnName("by_user_id");
                entity.Property(a => a.Note).HasColumnName("note");
                entity.Property(a => a.Timestamp).HasColumnName("timestamp");

                entity.HasOne(a => a.WorkItem)
                      .WithMany()
                      .HasForeignKey(a => a.WorkItemId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.ByUser)
                      .WithMany()
                      .HasForeignKey(a => a.ByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.FromUser)
                      .WithMany()
                      .HasForeignKey(a => a.FromUserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.ToUser)
                      .WithMany()
                      .HasForeignKey(a => a.ToUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<UserOtp>(entity =>
            {
                entity.ToTable("user_otps");
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Id).HasColumnName("id");
                entity.Property(o => o.Email).HasColumnName("email");
                entity.Property(o => o.OtpCode).HasColumnName("otp_code");
                entity.Property(o => o.Purpose).HasColumnName("purpose");
                entity.Property(o => o.ExpiryTime).HasColumnName("expiry_time");
                entity.Property(o => o.CreatedAt).HasColumnName("created_at");
            });
        }
    }
}
