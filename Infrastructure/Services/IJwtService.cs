using NoteApi.Common.Models;

namespace NoteApi.Infrastructure.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
