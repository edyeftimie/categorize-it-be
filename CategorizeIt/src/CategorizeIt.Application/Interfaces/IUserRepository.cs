using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Interfaces;

public interface IUserRepository
{
    Task CreateUserAsync(User user);
    Task<User?> GetUserByEmailAsync(string email);
}