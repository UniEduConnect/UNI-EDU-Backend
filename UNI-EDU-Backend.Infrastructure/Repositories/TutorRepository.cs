using UNI_EDU_Backend.Domain.Interfaces;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories
{
    public class TutorRepository : GenericRepository<Tutor>, ITutorRepository
    {
        public TutorRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}