using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.UnitTests.Infrastructure.Http;

internal sealed class HttpMockSequenceHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _queue = new();

    public int PendingHandlers => _queue.Count;

    public void EnqueueResponse(HttpResponseMessage response)
        => Enqueue((_, _) => Task.FromResult(response));

    public void Enqueue(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => _queue.Enqueue(handler);

    public void Enqueue(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        => _queue.Enqueue((request, _) => handler(request));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_queue.Count == 0)
        {
            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        }

        return _queue.Dequeue().Invoke(request, cancellationToken);
    }
}
