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
        private readonly IDomainEventDispatcher? _domainEventDispatcher;
        private bool _dispatchingDomainEvents;

        public DatabaseContext(DbContextOptions options, IDateTimeProvider dateTimeProvider) : this(options, dateTimeProvider, null)
        {
        }

        public DatabaseContext(
            DbContextOptions options,
            IDateTimeProvider dateTimeProvider,
            IDomainEventDispatcher? domainEventDispatcher) : base(options)
        {
            _dateTimeProvider = dateTimeProvider;
            _domainEventDispatcher = domainEventDispatcher;
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
        public DbSet<ZoneGroup> ZoneGroups { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<ZoneDeliveryRate> ZoneDeliveryRates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations from the assembly
            // This will automatically pick up all IEntityTypeConfiguration implementations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DatabaseContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAudit();

            if (_dispatchingDomainEvents || _domainEventDispatcher is null)
                return await base.SaveChangesAsync(cancellationToken);

            var startedTransaction = false;
            if (Database.CurrentTransaction is null && Database.IsRelational())
            {
                await Database.BeginTransactionAsync(cancellationToken);
                startedTransaction = true;
            }

            try
            {
                var result = await base.SaveChangesAsync(cancellationToken);

                var events = CollectAndDequeueDomainEvents();
                if (events.Count == 0)
                {
                    if (startedTransaction)
                        await Database.CurrentTransaction!.CommitAsync(cancellationToken);
                    return result;
                }

                // Order / OrderVehicle rows are saved first. Journals are posted only after that succeeds.
                _dispatchingDomainEvents = true;
                try
                {
                    await _domainEventDispatcher.DispatchAsync(events, cancellationToken);
                    if (ChangeTracker.HasChanges())
                    {
                        ApplyAudit();
                        result += await base.SaveChangesAsync(cancellationToken);
                    }
                }
                finally
                {
                    _dispatchingDomainEvents = false;
                }

                if (startedTransaction)
                    await Database.CurrentTransaction!.CommitAsync(cancellationToken);

                return result;
            }
            catch
            {
                if (startedTransaction && Database.CurrentTransaction is not null)
                    await Database.CurrentTransaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<DbResult> SaveChangesAsyncWithResult(CancellationToken cancellationToken = default)
        {
            try
            {
                await SaveChangesAsync(cancellationToken);
                return new DbResult { IsSuccess = true };
            }
            catch (Exception exp)
            {
                return new DbResult { IsSuccess = false, ErrorMessage = exp.Message };
            }
        }

        private void ApplyAudit()
        {
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
        }

        private List<IDomainEvent> CollectAndDequeueDomainEvents()
        {
            var events = new List<IDomainEvent>();
            foreach (var entry in ChangeTracker.Entries<AggregateRoot>())
            {
                if (entry.State == EntityState.Detached)
                    continue;

                events.AddRange(entry.Entity.DequeueDomainEvents());
            }

            return events;
        }
    }
}
