using System.Reflection;
using System.Text.Json;
using KafkaFlow;
using KafkaFlow.Producers;
using KafkaFlowExample.Attributes;
using Message = KafkaFlowExample.Messages.Message;

namespace KafkaFlowExample.Producers;

public class TypedMessageProducer<TMessage>(IProducerAccessor _producerAccessor)
    : ITypedMessageProducer<TMessage>
    where TMessage : Message
{
    public async Task ProduceAsync(TMessage message)
    {
        var _producer = _producerAccessor.GetProducer(message.GetType()
            .GetCustomAttributes(typeof(MessageAttribute)).OfType<MessageAttribute>().First().ProducerName);

        await _producer.ProduceAsync(message.Key, JsonSerializer.Serialize(message));
    }
}