using KafkaFlowExample.Attributes;

namespace KafkaFlowExample.Messages;

[Message("TextMessageV1")]
public record TestMessageV1(string Key, string Text) : Message(Key);