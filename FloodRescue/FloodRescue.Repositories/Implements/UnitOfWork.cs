using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Implements
{
    //Tất cả các xử lý liên quan đến repository/database ở trên các tầng sẽ đều thông qua Unit of Work 
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FloodRescueDbContext _context;

        //định nghĩa các field
        private IBaseRepository<Category>? _categories;
        private IBaseRepository<CitizenNotification>? _citizenNotifications;
        private IBaseRepository<IncidentReport>? _incidentReports;
        private IBaseRepository<Inventory>? _inventories;
        private IBaseRepository<Notification>? _notifications;
        private IBaseRepository<ReliefItem>? _reliefItems;
        private IBaseRepository<ReliefOrderDetail>? _reliefOrderDetails;
        private IBaseRepository<ReliefOrder>? _reliefOrders;
        private IBaseRepository<RescueMission>? _rescueMissions;
        private IBaseRepository<RescueRequest>? _rescueRequests;
        private IBaseRepository<RescueRequestImage>? _rescueRequestImages;
        private IBaseRepository<RescueTeam>? _rescueTeams;
        private IBaseRepository<RescueTeamMember>? _rescueTeamMembers;
        private IBaseRepository<Role>? _roles;
        private IBaseRepository<User>? _users;
        private IBaseRepository<Warehouse>? _warehouses;
        private IBaseRepository<RefreshToken>? _refreshTokens;

        public UnitOfWork(FloodRescueDbContext context)
        {
            _context = context;
        }

        //nhận context từ DI Container xong chuyển vào base repository
        //Categories trong unit of work tương tác tương đương với 1 CategoryRepository kế thừa từ ICategoryRepository
        // Service gọi Categories của unit of work là đang gọi tới BaseRepository
        public IBaseRepository<Category> Categories => _categories ??= new BaseRepository<Category>(_context);

        public IBaseRepository<CitizenNotification> CitizenNotifications => _citizenNotifications ??= new BaseRepository<CitizenNotification>(_context);

        public IBaseRepository<IncidentReport> IncidentReports => _incidentReports ??= new BaseRepository<IncidentReport>(_context);

        public IBaseRepository<Inventory> Inventories => _inventories ??= new BaseRepository<Inventory>(_context);

        public IBaseRepository<Notification> Notifications => _notifications ??= new BaseRepository<Notification>(_context);

        public IBaseRepository<ReliefItem> ReliefItems => _reliefItems ??= new BaseRepository<ReliefItem>(_context);

        public IBaseRepository<ReliefOrderDetail> ReliefOrderDetails => _reliefOrderDetails ??= new BaseRepository<ReliefOrderDetail>(_context);

        public IBaseRepository<ReliefOrder> ReliefOrders => _reliefOrders ??= new BaseRepository<ReliefOrder>(_context);

        public IBaseRepository<RescueMission> RescueMissions => _rescueMissions ??= new BaseRepository<RescueMission>(_context);

        public IBaseRepository<RescueRequest> RescueRequests => _rescueRequests ??= new BaseRepository<RescueRequest>(_context);

        public IBaseRepository<RescueRequestImage> RescueRequestImages => _rescueRequestImages ??= new BaseRepository<RescueRequestImage>(_context);

        public IBaseRepository<RescueTeam> RescueTeams => _rescueTeams ??= new BaseRepository<RescueTeam>(_context);

        public IBaseRepository<RescueTeamMember> RescueTeamMembers => _rescueTeamMembers ??= new BaseRepository<RescueTeamMember>(_context);

        public IBaseRepository<Role> Roles => _roles ??= new BaseRepository<Role>(_context);

        public IBaseRepository<User> Users => _users ??= new BaseRepository<User>(_context);

        public IBaseRepository<Warehouse> Warehouses => _warehouses ??= new BaseRepository<Warehouse>(_context);

        public IBaseRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new BaseRepository<RefreshToken>(_context);


        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
