using DPD.Application.Common.Interfaces;
using DPD.Infrastructure.Persistence;
using DPD.Infrastructure.Persistence.Interceptors;
using DPD.Infrastructure.Persistence.Repositories;
using DPD.Infrastructure.Security;
using DPD.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DPD.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var authMode = configuration["Authentication:Mode"] ?? "Mock";
        if (string.Equals(authMode, "Entra", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IUserService, EntraIdUserService>();
        }
        else
        {
            services.AddScoped<IUserService, MockUserService>();
        }
        services.AddScoped<AuditInterceptor>();

        var connectionString = configuration.GetConnectionString("Sqlite") ?? "Data Source=dpd-poc.db";

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<IStudyRepository, StudyRepository>();

        services.AddScoped<ICapacityPlanningService, CapacityPlanningService>();
        services.AddScoped<IReportingReadService, ReportingReadService>();
        services.AddHostedService<MaintenanceAlertWorker>();

        return services;
    }
}
