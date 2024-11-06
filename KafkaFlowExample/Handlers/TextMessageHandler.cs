using KafkaFlow;
using KafkaFlowExample.Messages;

namespace KafkaFlowExample.Handlers;

public class TextMessageHandler : IMessageHandler<TestMessageV1>
{
    public async Task Handle(IMessageContext context, TestMessageV1 messageV1)
    {
        Console.WriteLine($"Consumed Message: {messageV1.Text}");
    }
}