namespace NServiceBus.Persistence.ServiceFabric
{
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Outbox;
    using Outbox;
    using Persistence;
    using Transport;

    class ServiceFabricTransactionalSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            var inMemOutboxTransaction = transaction as ServiceFabricOutboxTransaction;
            if (inMemOutboxTransaction != null)
            {
                CompletableSynchronizedStorageSession session = new ServiceFabricSynchronizedStorageSession(null);
                return Task.FromResult(session);
            }
            return EmptyTask;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            return EmptyTask;
        }

        static readonly Task<CompletableSynchronizedStorageSession> EmptyTask = Task.FromResult<CompletableSynchronizedStorageSession>(null);
    }
}