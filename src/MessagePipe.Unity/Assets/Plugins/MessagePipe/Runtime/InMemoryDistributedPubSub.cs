#if !UNITY_2018_3_OR_NEWER

using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    public sealed class InMemoryDistributedPublisher<TKey, TMessage> : IDistributedPublisher<TKey, TMessage>
        where TKey : notnull
    {
        readonly IAsyncPublisher<TKey, TMessage> publisher;

        public InMemoryDistributedPublisher(IAsyncPublisher<TKey, TMessage> publisher)
        {
            this.publisher = publisher;
        }

        public UniTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default)
        {
            return publisher.PublishAsync(key, message, cancellationToken);
        }
    }

    public sealed class InMemoryDistributedSubscriber<TKey, TMessage> : IDistributedSubscriber<TKey, TMessage>
        where TKey : notnull
    {
        readonly IAsyncSubscriber<TKey, TMessage> subscriber;

        public InMemoryDistributedSubscriber(IAsyncSubscriber<TKey, TMessage> subscriber)
        {
            this.subscriber = subscriber;
        }

        public UniTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, CancellationToken cancellationToken = default)
        {
            var d = subscriber.Subscribe(key, new AsyncMessageHandlerBrdiger<TMessage>(handler));
            return new UniTask<IAsyncDisposable>(new AsyncDisposableBridge(d));
        }

        public UniTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, MessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {
            var d = subscriber.Subscribe(key, new AsyncMessageHandlerBrdiger<TMessage>(handler), filters.Select(x => new AsyncMessageHandlerFilterBrdiger<TMessage>(x)).ToArray());
            return new UniTask<IAsyncDisposable>(new AsyncDisposableBridge(d));
        }

        public UniTask<IAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, CancellationToken cancellationToken = default)
        {
            var d = subscriber.Subscribe(key, handler);
            return new UniTask<IAsyncDisposable>(new AsyncDisposableBridge(d));
        }

        public UniTask<IAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, AsyncMessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {

            var d = subscriber.Subscribe(key, handler, filters);
            return new UniTask<IAsyncDisposable>(new AsyncDisposableBridge(d));
        }
    }

    internal sealed class AsyncDisposableBridge : IAsyncDisposable
    {
        readonly IDisposable disposable;

        public AsyncDisposableBridge(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public UniTask DisposeAsync()
        {
            disposable.Dispose();
            return default;
        }
    }

    internal sealed class AsyncMessageHandlerBrdiger<T> : IAsyncMessageHandler<T>
    {
        readonly IMessageHandler<T> handler;

        public AsyncMessageHandlerBrdiger(IMessageHandler<T> handler)
        {
            this.handler = handler;
        }

        public UniTask HandleAsync(T message, CancellationToken cancellationToken)
        {
            handler.Handle(message);
            return default;
        }
    }

    internal sealed class AsyncMessageHandlerFilterBrdiger<T> : AsyncMessageHandlerFilter<T>
    {
        readonly MessageHandlerFilter<T> filter;

        public AsyncMessageHandlerFilterBrdiger(MessageHandlerFilter<T> filter)
        {
            this.filter = filter;
            this.Order = filter.Order;
        }

        public override UniTask HandleAsync(T message, CancellationToken cancellationToken, Func<T, CancellationToken, UniTask> next)
        {
            filter.Handle(message, async x => await next(x, CancellationToken.None));
            return default;
        }
    }
}

#endif