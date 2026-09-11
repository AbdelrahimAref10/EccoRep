using Domain.Common;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class DatabaseContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public DatabaseContext(DbContextOptions options, IDateTimeProvider dateTimeProvider) : base(options)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<TieredDiscount> TieredDiscounts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderVehicle> OrderVehicles { get; set; }
        public DbSet<ReservedVehiclesPerDays> ReservedVehiclesPerDays { get; set; }
        public DbSet<OrderPayment> OrderPayments { get; set; }
        public DbSet<CustomerWallet> CustomerWallets { get; set; }
        public DbSet<RefundablePaypalAmount> RefundablePaypalAmounts { get; set; }
        public DbSet<CompanyTreasury> CompanyTreasuries { get; set; }
        public DbSet<OrderTotals> OrderTotals { get; set; }
        public DbSet<CustomerLocation> CustomerLocations { get; set; }
        public DbSet<AdminNotification> AdminNotifications { get; set; }
        public DbSet<MerchantNotification> MerchantNotifications { get; set; }
        public DbSet<Support> Supports { get; set; }
        public DbSet<MerchantOrder> MerchantOrders { get; set; }
        public DbSet<MerchantOrderPaymentDetail> MerchantOrderPaymentDetails { get; set; }
        public DbSet<DeliveryMenOrder> DeliveryMenOrders { get; set; }
        public DbSet<DeliveryOrderPaymentDetail> DeliveryOrderPaymentDetails { get; set; }
        public DbSet<OrderJournal> OrderJournals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations from the assembly
            // This will automatically pick up all IEntityTypeConfiguration implementations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DatabaseContext).Assembly);
        }

        public async Task<DbResult> SaveChangesAsyncWithResult(CancellationToken cancellationToken = default)
        {
            try
            {
                // Track changes for auditing
                foreach (var entry in ChangeTracker.Entries<IAuditable>())
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entry.Entity.CreatedDate = _dateTimeProvider.Now;
                            break;
                        case EntityState.Modified:
                            entry.Entity.LastModifiedDate = _dateTimeProvider.Now;
                            break;
                    }
                }

                await base.SaveChangesAsync(cancellationToken);

                return new DbResult { IsSuccess = true };
            }
            catch (Exception exp)
            {
                return new DbResult { IsSuccess = false, ErrorMessage = exp.Message };
            }
        }


    }
}
