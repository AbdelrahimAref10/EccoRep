using Domain.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.Common
{
    public static class CustomerLookup
    {
        public static Task<Customer?> ByUserIdAsync(
            DatabaseContext context,
            int userId,
            CancellationToken cancellationToken,
            bool asTracking = false)
        {
            var query = asTracking
                ? context.Customers.AsTracking()
                : context.Customers.AsNoTracking();

            return query.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        }

        public static Task<Customer?> ByUserIdWithCityAsync(
            DatabaseContext context,
            int userId,
            CancellationToken cancellationToken)
        {
            return context.Customers
                .Include(c => c.City)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        }
    }
}
