using KafkaFlowExample.Messages;

namespace KafkaFlowExample.Producers;

public interface ITypedMessageProducer<in TMessage>
    where TMessage : Message
{
    Task ProduceAsync(TMessage message);
}