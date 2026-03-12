using InventoryManager.Application.Common.Interfaces;
using InventoryManager.Application.Interfaces;
using InventoryManager.Infrastructure.Persistence;
using InventoryManager.Infrastructure.Persistence.Repositories;
using InventoryManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["DATABASE_URL"] ??
            configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<InventoryManagerDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IItemLikeRepository, ItemLikeRepository>();
        services.AddScoped<IDiscussionRepository, DiscussionRepository>();
        services.AddScoped<IInventoryAccessService, InventoryAccessService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ICustomIdService, CustomIdService>();
        services.AddScoped<IEmailSender, EmailSender>();
        
        return services;
    }
}