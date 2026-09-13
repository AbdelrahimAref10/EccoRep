namespace Domain.Common
{
    /// <summary>Base for aggregates that raise domain events.</summary>
    public abstract class AggregateRoot
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents() => _domainEvents.Clear();

        public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
        {
            var copy = _domainEvents.ToList();
            _domainEvents.Clear();
            return copy;
        }
    }
}
