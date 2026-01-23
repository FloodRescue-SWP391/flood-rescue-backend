using FloodRescue.Repositories.Entites;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Context
{
    public class FloodRescueDbContext : DbContext
    {
        public FloodRescueDbContext(DbContextOptions<FloodRescueDbContext> options) : base(options) { }


        #region DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }  

        public DbSet<RescueTeam> RescueTeams { get; set; }
        public DbSet<RescueTeamMember> RescueTeamMembers { get; set; }
        public DbSet<RescueRequest> RescueRequests { get; set; }
        public DbSet<RescueMission> RescueMissions { get; set; }    
        public DbSet<IncidentReport> IncidentReports { get; set; }

        public DbSet<Category> Categories { get; set; }
        public DbSet<ReliefItem> ReliefItems { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<ReliefOrder> ReliefOrders { get; set; }
        public DbSet<ReliefOrderDetail> ReliefOrderDetails { get; set; }

        public DbSet<Notification> Notifications { get; set; }
        public DbSet<CitizenNotification> CitizenNotifications { get; set; }
        public DbSet<RescueRequestImage> RescueRequestImages { get; set; }
        #endregion



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure cascade delete behaviors to prevent cycles
            
            // ReliefOrders - Prevent cascade delete conflicts
            modelBuilder.Entity<ReliefOrder>()
                .HasOne(ro => ro.Manager)
                .WithMany()
                .HasForeignKey(ro => ro.ManagerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReliefOrder>()
                .HasOne(ro => ro.Warehouse)
                .WithMany()
                .HasForeignKey(ro => ro.WarehouseID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReliefOrder>()
                .HasOne(ro => ro.RescueTeam)
                .WithMany()
                .HasForeignKey(ro => ro.RescueTeamID)
                .OnDelete(DeleteBehavior.Restrict);

            // IncidentReports - Prevent cascade delete conflicts
            modelBuilder.Entity<IncidentReport>()
                .HasOne(ir => ir.Reported)
                .WithMany()
                .HasForeignKey(ir => ir.ReportedID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IncidentReport>()
                .HasOne(ir => ir.Resolver)
                .WithMany()
                .HasForeignKey(ir => ir.ResolvedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Warehouse - Prevent cascade delete conflicts
            modelBuilder.Entity<Warehouse>()
                .HasOne(w => w.Manager)
                .WithMany()
                .HasForeignKey(w => w.ManagerID)
                .OnDelete(DeleteBehavior.Restrict);

            // RescueRequest - Prevent cascade delete conflicts
            modelBuilder.Entity<RescueRequest>()
                .HasOne(rr => rr.Coordinator)
                .WithMany()
                .HasForeignKey(rr => rr.CoordinatorID)
                .OnDelete(DeleteBehavior.Restrict);

            // RescueTeamMember - Prevent cascade delete conflicts
            modelBuilder.Entity<RescueTeamMember>()
                .HasOne(rtm => rtm.User)
                .WithMany()
                .HasForeignKey(rtm => rtm.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RescueRequestImage>()
                .HasOne(rri => rri.RescueRequest)
                .WithMany()
                .HasForeignKey(rri => rri.RescueRequestID)
                .OnDelete(DeleteBehavior.Restrict);

            // [RegularExpression("^(AD|RC|IM|RT)$")]
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = "AD", RoleName = "Admin", IsDeleted = false },
                new Role { RoleID = "RC", RoleName = "Rescue Coordinator", IsDeleted = false }, 
                new Role { RoleID = "IM", RoleName = "Inventory Manager", IsDeleted = false },
                new Role { RoleID = "RT", RoleName = "Rescue Team Member", IsDeleted = false }  
            );
        }
    }
}
