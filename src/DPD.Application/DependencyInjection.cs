using DPD.Application.Common.Behaviours;
using FluentValidation;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DPD.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));

        // Mapster : la configuration (IRegister) est scannée une seule fois au démarrage et
        // compilée en expressions, évitant toute réflexion à l'exécution lors des mappings.
        var mapperConfig = TypeAdapterConfig.GlobalSettings;
        mapperConfig.Scan(typeof(DependencyInjection).Assembly);
        services.AddSingleton(mapperConfig);
        services.AddSingleton<IMapper, ServiceMapper>();

        return services;
    }
}


