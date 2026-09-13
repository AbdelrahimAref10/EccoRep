namespace Domain.Common
{
    /// <summary>
    /// Publishes domain events after the originating aggregate changes have been saved.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
    }
}
