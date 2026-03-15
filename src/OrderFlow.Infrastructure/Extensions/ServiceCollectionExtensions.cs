using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Infrastructure.Data;
using OrderFlow.Infrastructure.Messaging;
using OrderFlow.Infrastructure.Repositories;

namespace OrderFlow.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrderFlowDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(OrderFlowDbContext).Assembly.FullName)));

        services.Configure<RabbitMqOptions>(options =>
            configuration.GetSection(RabbitMqOptions.SectionName).Bind(options));

        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
