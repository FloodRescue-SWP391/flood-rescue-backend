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
        // Lấy danh sách và có thêm 1 số tiêu chí lọc, còn không lọc thì để null mặc định
        Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? filter = null);

        Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> filter);

        Task<TEntity?> GetByIdAsync(object id);

        Task AddAsync(TEntity entity);

        //Vế này để unit of work làm mình kh cần phải await dbcontext.SaveChangesAsync() sau mỗi thao tác nữa nên là chỉ cần để void cập nhật trong ram là dủ
        void Update(TEntity entity);
        void Delete(TEntity entity);    
    }
}
