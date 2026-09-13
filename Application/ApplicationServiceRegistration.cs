using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));

            services.AddScoped<Features.AdminReport.Export.IReportExportService, Features.AdminReport.Export.ReportExportService>();
            services.AddScoped<Features.Order.Services.IVehicleReservationQueryService, Features.Order.Services.VehicleReservationQueryService>();
            services.AddScoped<Features.Order.Services.IOrderJournalService, Features.Order.Services.OrderJournalService>();
            services.AddScoped<Features.Order.Services.IOrderRealtimeNotifier, Features.Order.Services.OrderRealtimeNotifier>();
            services.AddScoped<Domain.Common.IDomainEventDispatcher, Common.DomainEvents.DomainEventDispatcher>();

            services.AddScoped<Features.Customer.Command.AdminCreateCustomerCommand.AdminCreateCustomerCommandValidator>();
            services.AddScoped<Features.Customer.Command.UpdateCustomerCommand.UpdateCustomerCommandValidator>();
            services.AddScoped<Features.Auth.Command.LoginCommand.LoginCommandValidator>();
            services.AddScoped<Features.Auth.Command.RegisterCommand.RegisterCommandValidator>();
            services.AddScoped<Features.Auth.Command.ForgetPasswordCommand.ForgetPasswordCommandValidator>();
            services.AddScoped<Features.Auth.Command.ResetPasswordCommand.ResetPasswordCommandValidator>();
            services.AddScoped<Features.City.Command.AddCityCommand.AddCityCommandValidator>();
            services.AddScoped<Features.City.Command.UpdateCityCommand.UpdateCityCommandValidator>();
            services.AddScoped<Features.Category.Command.CreateCategoryCommand.CreateCategoryCommandValidator>();
            services.AddScoped<Features.Category.Command.UpdateCategoryCommand.UpdateCategoryCommandValidator>();
            services.AddScoped<Features.SubCategory.Command.CreateSubCategoryCommand.CreateSubCategoryCommandValidator>();
            services.AddScoped<Features.SubCategory.Command.UpdateSubCategoryCommand.UpdateSubCategoryCommandValidator>();
            services.AddScoped<Features.Support.Command.CreateSupportCommand.CreateSupportCommandValidator>();
            services.AddScoped<Features.Support.Command.UpdateSupportCommand.UpdateSupportCommandValidator>();

            return services;
        }
    }
}
