using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.MarkCustomerRejectedReceiptCommand
{
    public record MarkCustomerRejectedReceiptCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public FaultParty FaultParty { get; set; }
        public string? Note { get; set; }
    }

    public class MarkCustomerRejectedReceiptCommandHandler
        : IRequestHandler<MarkCustomerRejectedReceiptCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderJournalService _journal;
        private readonly IOrderRealtimeNotifier _realtime;

        public MarkCustomerRejectedReceiptCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderJournalService journal,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _journal = journal;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(
            MarkCustomerRejectedReceiptCommand request,
            CancellationToken cancellationToken)
        {
            if (request.FaultParty == FaultParty.None)
                return Result.Failure<bool>("Fault party is required");

            var order = await _context.Orders
                .AsTracking()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            var createdBy = _userSession.UserName ?? "System";

            try
            {
                order.MarkCustomerRejectedReceipt(request.FaultParty, request.Note, createdBy);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<bool>(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<bool>(ex.Message);
            }

            if (request.FaultParty == FaultParty.Merchant)
            {
                var clawback = await ClawbackPartyBalancesAsync(
                    request.OrderId,
                    LedgerPartyType.Merchant,
                    request.FaultParty,
                    request.Note,
                    createdBy,
                    cancellationToken);
                if (clawback.IsFailure)
                    return Result.Failure<bool>(clawback.Error);
            }
            else if (request.FaultParty == FaultParty.Delivery)
            {
                var clawback = await ClawbackPartyBalancesAsync(
                    request.OrderId,
                    LedgerPartyType.Delivery,
                    request.FaultParty,
                    request.Note,
                    createdBy,
                    cancellationToken);
                if (clawback.IsFailure)
                    return Result.Failure<bool>(clawback.Error);
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                request.OrderId,
                "Customer rejected receipt",
                $"Customer rejected receipt on order {request.OrderId}.",
                NotificationType.OrderUpdated,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }

        private async Task<Result> ClawbackPartyBalancesAsync(
            int orderId,
            LedgerPartyType partyType,
            FaultParty faultParty,
            string? note,
            string createdBy,
            CancellationToken cancellationToken)
        {
            var entries = await _journal.GetOrderEntriesAsync(orderId, cancellationToken);
            foreach (var group in entries.Where(j => j.PartyType == partyType && j.PartyId.HasValue).GroupBy(j => j.PartyId!.Value))
            {
                var partyId = group.Key;
                var balance = await _journal.GetPartyBalanceAsync(orderId, partyType, partyId, cancellationToken);
                if (balance <= 0)
                    continue;

                var key = OrderJournalKeys.Build(
                    orderId,
                    $"fault-clawback:{partyType.ToString().ToLowerInvariant()}:{partyId}:{System.Guid.NewGuid():N}");

                var post = await _journal.PostDebitAsync(
                    orderId,
                    partyType,
                    partyId,
                    balance,
                    OrderJournalEntryKind.FaultClawback,
                    key,
                    note: note,
                    faultParty: faultParty,
                    createdBy: createdBy,
                    cancellationToken: cancellationToken);

                if (post.IsFailure)
                    return post;
            }

            return Result.Success();
        }
    }
}
