namespace UNI_EDU_Backend.Application.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        Task SaveChangesAsync();
    }
}