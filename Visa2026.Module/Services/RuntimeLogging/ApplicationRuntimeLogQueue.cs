using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogQueue
{
    private readonly Channel<ApplicationRuntimeLogEntry> channel;

    public ApplicationRuntimeLogQueue(IOptions<ApplicationRuntimeLogOptions> options)
    {
        int capacity = options.Value.QueueCapacity <= 0 ? 1000 : options.Value.QueueCapacity;
        channel = Channel.CreateBounded<ApplicationRuntimeLogEntry>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public bool TryEnqueue(ApplicationRuntimeLogEntry entry) => channel.Writer.TryWrite(entry);

    public ChannelReader<ApplicationRuntimeLogEntry> Reader => channel.Reader;
}
