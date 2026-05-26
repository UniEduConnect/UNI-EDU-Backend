using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories
{
    public interface IStudentRepository : IGenericRepository<Student>
    {
        Task<List<Student>> GetByParentIdAsync(Guid parentId, CancellationToken cancellationToken);
    }
}
