using Application.Features.AdminHome.Common;
using Application.Features.AdminHome.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AdminHome.Query.GetHomePaymentsMixQuery
{
    public record GetHomePaymentsMixQuery : IRequest<Result<HomePaymentsMixDto>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? CityId { get; set; }
    }

    public class GetHomePaymentsMixQueryHandler : IRequestHandler<GetHomePaymentsMixQuery, Result<HomePaymentsMixDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GetHomePaymentsMixQueryHandler(DatabaseContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<HomePaymentsMixDto>> Handle(GetHomePaymentsMixQuery request, CancellationToken cancellationToken)
        {
            var now = _dateTimeProvider.Now;
            var (from, to) = AdminHomeDateRange.Resolve(request.From, request.To, now);

            var payments = _context.OrderPayments.AsNoTracking()
                .Where(p => p.CreatedDate >= from && p.CreatedDate <= to);

            if (request.CityId.HasValue)
            {
                payments = payments.Where(p => p.Order.CityId == request.CityId.Value);
            }

            var paidCount = await payments.CountAsync(p => p.State == PaymentState.Paid, cancellationToken);
            var pendingCount = await payments.CountAsync(p => p.State == PaymentState.Pending, cancellationToken);
            var failedCount = await payments.CountAsync(p => p.State == PaymentState.Failed, cancellationToken);
            var refundedCount = await payments.CountAsync(p => p.State == PaymentState.Refunded, cancellationToken);
            var totalPaid = await payments.Where(p => p.State == PaymentState.Paid).SumAsync(p => (decimal?)p.Total, cancellationToken) ?? 0;
            var refundedAmount = await payments.Where(p => p.State == PaymentState.Refunded).SumAsync(p => (decimal?)p.Total, cancellationToken) ?? 0;

            var methodGroups = await payments
                .Where(p => p.State == PaymentState.Paid)
                .GroupBy(p => p.PaymentMethodId)
                .Select(g => new
                {
                    MethodId = g.Key,
                    OrderCount = g.Count(),
                    Amount = g.Sum(x => x.Total)
                })
                .ToListAsync(cancellationToken);

            var methods = methodGroups
                .Select(g => new HomePaymentMethodSliceDto
                {
                    MethodId = g.MethodId,
                    MethodName = Enum.IsDefined(typeof(PaymentMethod), g.MethodId)
                        ? ((PaymentMethod)g.MethodId).ToString()
                        : $"Method {g.MethodId}",
                    OrderCount = g.OrderCount,
                    Amount = g.Amount,
                    Percentage = totalPaid == 0 ? 0 : Math.Round(g.Amount / totalPaid * 100m, 2)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            return Result.Success(new HomePaymentsMixDto
            {
                TotalPaidAmount = totalPaid,
                PaidCount = paidCount,
                PendingCount = pendingCount,
                FailedCount = failedCount,
                RefundedCount = refundedCount,
                RefundedAmount = refundedAmount,
                Methods = methods,
                From = from,
                To = to
            });
        }
    }
}
