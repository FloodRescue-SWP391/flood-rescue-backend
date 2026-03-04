using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Interface
{
    public interface IBaseRepository<TEntity> where TEntity : class
    {
        // ===== BASIC METHODS (KHÔNG CÓ INCLUDE) =====
        
        /// <summary>
        /// Lấy danh sách và có thêm 1 số tiêu chí lọc, còn không lọc thì để null mặc định
        /// </summary>
        Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? filter = null);

        /// <summary>
        /// Lấy 1 entity theo filter
        /// </summary>
        Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> filter);

        /// <summary>
        /// Lấy entity theo ID (không load navigation properties)
        /// </summary>
        Task<TEntity?> GetByIdAsync(object id);

        Task AddAsync(TEntity entity);

        //Vế này để unit of work làm mình kh cần phải await dbcontext.SaveChangesAsync() sau mỗi thao tác nữa nên là chỉ cần để void cập nhật trong ram là dủ
        void Update(TEntity entity);
        void Delete(TEntity entity);

        // ===== OVERLOAD METHODS (CÓ INCLUDE NAVIGATION PROPERTIES) =====

        /// <summary>
        /// Lấy 1 entity theo filter VỚI navigation properties
        /// Ví dụ: GetAsync(u => u.UserID == id, u => u.Role, u => u.RefreshTokens)
        /// </summary>
        Task<TEntity?> GetAsync(
            Expression<Func<TEntity, bool>> filter,
            params Expression<Func<TEntity, object>>[] includes);

        /// <summary>
        /// Lấy danh sách entity VỚI navigation properties
        /// Ví dụ: GetAllAsync(u => u.IsDeleted == false, u => u.Role)
        /// </summary>
        Task<List<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? filter,
            params Expression<Func<TEntity, object>>[] includes);
    }
}
