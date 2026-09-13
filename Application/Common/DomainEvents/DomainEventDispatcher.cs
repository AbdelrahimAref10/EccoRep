using Domain.Common;
using Domain.Events;
using Domain.Models;
using MediatR;

namespace Application.Common.DomainEvents
{
    /// <summary>Publishes domain events as MediatR notifications after SaveChanges.</summary>
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IMediator _mediator;

        public DomainEventDispatcher(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in events)
            {
                await _mediator.Publish(new DomainEventNotification(domainEvent), cancellationToken);
            }
        }
    }

    public sealed class DomainEventNotification : INotification
    {
        public IDomainEvent DomainEvent { get; }

        public DomainEventNotification(IDomainEvent domainEvent)
        {
            DomainEvent = domainEvent;
        }
    }

    /// <summary>Posts journal lines carried by domain events after the source row is saved.</summary>
    public class OrderLedgerDomainEventHandler : INotificationHandler<DomainEventNotification>
    {
        private readonly Features.Order.Services.IOrderJournalService _journal;

        public OrderLedgerDomainEventHandler(Features.Order.Services.IOrderJournalService journal)
        {
            _journal = journal;
        }

        public async Task Handle(DomainEventNotification notification, CancellationToken cancellationToken)
        {
            var (lines, createdBy) = notification.DomainEvent switch
            {
                OrderVehicleReceivedFromOwnerEvent e => (e.Lines, e.CreatedBy),
                OrderVehicleDeliveredToCustomerEvent e => (e.Lines, e.CreatedBy),
                OrderLedgerPostsRequested e => (e.Lines, e.CreatedBy),
                _ => ((IReadOnlyList<OrderLedgerLine>?)null, (string?)null)
            };

            if (lines == null || lines.Count == 0)
                return;

            var result = await _journal.PostLinesAsync(lines, createdBy, cancellationToken);
            if (result.IsFailure)
                throw new InvalidOperationException(result.Error);
        }
    }
}
