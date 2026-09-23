using System.Threading.Channels;
using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Interfaces;

namespace AsisyaApi.Infrastructure.BulkLoad;

/// <summary>
/// Cola en memoria (productor: POST /Product/bulk, consumidor: <see cref="BulkLoadBackgroundService"/>).
/// Se registra como singleton para que productor y consumidor compartan la misma instancia.
/// </summary>
public class BulkLoadChannel : IBulkLoadQueue
{
    public const int Capacity = 100;

    private readonly Channel<BulkLoadWorkItem> _channel = Channel.CreateBounded<BulkLoadWorkItem>(
        new BoundedChannelOptions(Capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    public ChannelReader<BulkLoadWorkItem> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(BulkLoadWorkItem item, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(item, ct);
}
