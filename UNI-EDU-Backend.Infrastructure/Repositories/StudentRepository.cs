using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories
{
    public class StudentRepository : GenericRepository<Student>, IStudentRepository
    {
        public StudentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Student>> GetByParentIdAsync(Guid parentId, CancellationToken cancellationToken)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(s => s.ParentID == parentId)
                .OrderBy(s => s.FullName)
                .ToListAsync(cancellationToken);
        }
    }
}
