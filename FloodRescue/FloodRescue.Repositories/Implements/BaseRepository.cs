using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Implements
{
    //1 Generic Repository cho tất cả các entity - có thể thao tác được trên đó
    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
    {
        private readonly FloodRescueDbContext _context;  
        private readonly DbSet<TEntity> _dbSet;

        //public class MissionRepository : BaseRepository<RescueMission>
        public BaseRepository(FloodRescueDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }

        //Để cho các class kế thừa có thể sử dụng được
        // Chủ yếu dùng để mở rộng tính năng riêng của class đó
        protected FloodRescueDbContext Context => _context;
        protected DbSet<TEntity> DbSet => _dbSet;   

        public async Task AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Delete(TEntity entity)
        {
            _dbSet.Remove(entity); 
        }

        public async Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? filter = null)
        {
            if (filter == null)
            {
                return await _dbSet.ToListAsync();
            }

            return await _dbSet.Where(filter).ToListAsync();
        }

        public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> filter)
        {
            return await _dbSet.FirstOrDefaultAsync(filter);
        }

        public async Task<TEntity?> GetByIdAsync(object id)
        {
           return await _dbSet.FindAsync(id);
        }

        public void Update(TEntity entity)
        {
            _dbSet.Update(entity);
        }

        // ===== OVERLOAD METHOD WITH INCLUDE SUPPORT =====
        
        /// <summary>
        /// Lấy 1 entity theo filter VỚI navigation properties
        /// Ví dụ: GetAsync(u => u.UserID == id, u => u.Role, u => u.RefreshTokens)
        /// </summary>
        public async Task<TEntity?> GetAsync(
            Expression<Func<TEntity, bool>> filter, 
            params Expression<Func<TEntity, object>>[] includes) 
        {
            IQueryable<TEntity> query = _dbSet;

            // Thêm từng Include vào query
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            // FIX: Thêm await cho FirstOrDefaultAsync
            return await query.FirstOrDefaultAsync(filter);
        }

        /// <summary>
        /// Lấy danh sách entity VỚI navigation properties
        /// Ví dụ: GetAllAsync(u => u.IsDeleted == false, u => u.Role)
        /// </summary>
        public async Task<List<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? filter, 
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            // Thêm từng Include vào query
            foreach (var include in includes)
            {
                query = query.Include(include); 
            }

            if(filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync();
        }
    }
}
