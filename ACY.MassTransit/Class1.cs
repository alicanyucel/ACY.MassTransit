using System.Collections.Concurrent;

namespace ACY.Messaging;

// Mesaj kontratı
public interface IMessage
{
    Guid Id { get; }
    DateTime CreatedAt { get; }
}

// Consumer interface
public interface IConsumer<TMessage> where TMessage : IMessage
{
    Task ConsumeAsync(TMessage message, CancellationToken cancellationToken = default);
}

// Bus interface
public interface IBus
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IMessage;

    Task SubscribeAsync<TMessage>(IConsumer<TMessage> consumer, CancellationToken cancellationToken = default)
        where TMessage : IMessage;
}

// In-Memory Bus implementasyonu
public class InMemoryBus : IBus
{
    private readonly ConcurrentDictionary<Type, List<object>> _consumers = new();

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IMessage
    {
        if (_consumers.TryGetValue(typeof(TMessage), out var consumers))
        {
            var tasks = consumers
                .Cast<IConsumer<TMessage>>()
                .Select(c => c.ConsumeAsync(message, cancellationToken));

            return Task.WhenAll(tasks);
        }

        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TMessage>(IConsumer<TMessage> consumer, CancellationToken cancellationToken = default)
        where TMessage : IMessage
    {
        var list = _consumers.GetOrAdd(typeof(TMessage), _ => new List<object>());
        list.Add(consumer);
        return Task.CompletedTask;
    }
}

