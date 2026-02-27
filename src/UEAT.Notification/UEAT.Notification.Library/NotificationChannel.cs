using System.Threading.Channels;
using UEAT.Notification.Core;

namespace UEAT.Notification.Library;

public sealed class NotificationChannel
{
    private readonly Channel<INotification> _channel = Channel.CreateUnbounded<INotification>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelWriter<INotification> Writer => _channel.Writer;
    public ChannelReader<INotification> Reader => _channel.Reader;
}