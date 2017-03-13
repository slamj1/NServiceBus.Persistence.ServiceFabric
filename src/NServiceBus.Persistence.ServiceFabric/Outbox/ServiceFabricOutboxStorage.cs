﻿namespace NServiceBus.Persistence.ServiceFabric.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.ServiceFabric.Data;
    using NServiceBus.Outbox;

    class ServiceFabricOutboxStorage : IOutboxStorage
    {
        IReliableStateManager reliableStateManager;

        public ServiceFabricOutboxStorage(IReliableStateManager reliableStateManager)
        {
            this.reliableStateManager = reliableStateManager;
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            OutboxMessage result = null;
            using (var tx = reliableStateManager.CreateTransaction()) { 
                var storage = await reliableStateManager.Outbox(tx).ConfigureAwait(false);
                var conditionalValue = await storage.TryGetValueAsync(tx, messageId).ConfigureAwait(false);
                if (conditionalValue.HasValue)
                {
                    result = new OutboxMessage(messageId, conditionalValue.Value.TransportOperations.Select(o => new TransportOperation(o.MessageId, o.Options, o.Body, o.Headers)).ToArray());
                }
            }
            return result;
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            return Task.FromResult<OutboxTransaction>(new ServiceFabricOutboxTransaction(reliableStateManager));
        }

        public async Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var tx = ((ServiceFabricOutboxTransaction)transaction).Transaction;
            var storage = await reliableStateManager.Outbox(tx).ConfigureAwait(false);
            if (!await storage.TryAddAsync(tx, message.MessageId, new StoredOutboxMessage(message.MessageId, message.TransportOperations.Select(t => new StoredTransportOperation(t.MessageId, t.Options, t.Body, t.Headers)).ToArray())).ConfigureAwait(false))
            {
                throw new Exception($"Outbox message with id '{message.MessageId}' is already present in storage.");
            }
        }

        public async Task SetAsDispatched(string messageId, ContextBag context)
        {
            using (var tx = reliableStateManager.CreateTransaction())
            {
                var storage = await reliableStateManager.Outbox(tx).ConfigureAwait(false);
                var conditionalValue = await storage.TryGetValueAsync(tx, messageId).ConfigureAwait(false);
                if (conditionalValue.HasValue)
                {
                    var dispatched = conditionalValue.Value.CloneAndMarkAsDispatched();
                    await storage.SetAsync(tx, messageId, dispatched);

                    var cleanup = await reliableStateManager.OutboxCleanup(tx).ConfigureAwait(false);
                    await cleanup.EnqueueAsync(tx, new CleanupStoredOutboxCommand()
                    {
                        MessageId = messageId,
                        StoredAt = conditionalValue.Value.StoredAt
                    });

                }
                await tx.CommitAsync();
            }
        }

        internal async Task CleanupMessagesOlderThan(DateTimeOffset date)
        {
            using (var tx = reliableStateManager.CreateTransaction())
            {
                var queue = await reliableStateManager.OutboxCleanup(tx);
                var message = await queue.TryDequeueAsync(tx);
                if (message.HasValue)
                {
                    if (message.Value.StoredAt <= date)
                    {
                        var storage = await reliableStateManager.Outbox(tx);
                        await storage.TryRemoveAsync(tx, message.Value.MessageId);
                    }
                    else 
                    {
                        await queue.EnqueueAsync(tx, new CleanupStoredOutboxCommand
                        {
                            MessageId = message.Value.MessageId,
                            StoredAt = message.Value.StoredAt
                        });
                    }

                    await tx.CommitAsync();
                }
            }
        }

    }
}