using FloodRescue.Repositories.Entites;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Interface
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseRepository<Category> Categories { get; }
        IBaseRepository<CitizenNotification> CitizenNotifications { get; }
        IBaseRepository<IncidentReport> IncidentReports { get; }
        IBaseRepository<Inventory> Inventories { get; }
        IBaseRepository<Notification> Notifications { get; }
        IBaseRepository<ReliefItem> ReliefItems { get; }
        IBaseRepository<ReliefOrderDetail> ReliefOrderDetails { get; }
        IBaseRepository<ReliefOrder> ReliefOrders { get; }
        IBaseRepository<RescueMission> RescueMissions { get; }
        IBaseRepository<RescueRequest> RescueRequests { get; }
        IBaseRepository<RescueRequestImage> RescueRequestImages { get; }
        IBaseRepository<RescueTeam> RescueTeams { get; }
        IBaseRepository<RescueTeamMember> RescueTeamMembers { get; }
        IBaseRepository<Role> Roles { get; }
        IBaseRepository<User> Users { get; }
        IBaseRepository<Warehouse> Warehouses { get; }
        IBaseRepository<RefreshToken> RefreshTokens { get; }

        Task<IDbContextTransaction> BeginTransactionAsync();

        Task<int> SaveChangesAsync();

    }
}
