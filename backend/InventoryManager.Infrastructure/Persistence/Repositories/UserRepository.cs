using InventoryManager.Application.Interfaces;
using InventoryManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly InventoryManagerDbContext _db;

    public UserRepository(InventoryManagerDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return _db.Users.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return _db.Users.FirstOrDefaultAsync(x => x.Email == email);
    }

    public Task<User?> GetByUserNameAsync(string userName)
    {
        return _db.Users.FirstOrDefaultAsync(x => x.Name == userName);
    }

    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }

    public Task<List<User>> GetAllAsync()
    {
        return _db.Users
            .OrderBy(x => x.Email)
            .ToListAsync();
    }
}