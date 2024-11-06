namespace KafkaFlowExample.Attributes;

[AttributeUsage(validOn: AttributeTargets.Class, Inherited = false)]
public class MessageAttribute(string _topic, string? _producerName = null) : Attribute
{
    public string Topic => _topic;

    public string ProducerName => _producerName ?? _topic + "Producer";
}