using KafkaFlowExample.Attributes;

namespace KafkaFlowExample.Messages;

[Message("TextMessageV1")]
public class TestMessageV1 : Message
{
    public string Text { get; set; }
};