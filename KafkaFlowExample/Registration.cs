using System.Reflection;
using Dumpify;
using KafkaFlow.Configuration;
using KafkaFlowExample.Attributes;
using KafkaFlowExample.Messages;
using KafkaFlowExample.Producers;

namespace KafkaFlowExample;

public static class Registration
{
    private static readonly Type DefaultInterfaceType = typeof(ITypedMessageProducer<>);
    private static readonly Type DefaultType = typeof(TypedMessageProducer<>);
    private static readonly Type BaseMessage = typeof(Message);

    public static IClusterConfigurationBuilder AddProducers(this IClusterConfigurationBuilder cluster,
        params Type[] sourceTypes)
    {
        var assemblies = sourceTypes.Select(x => x.Assembly).Distinct().SelectMany(x => x.GetTypes())
            .Where(x => BaseMessage.IsAssignableFrom(x) && !x.IsAbstract).Select(x =>
            {
                var customAttribute =
                    x.GetCustomAttributes(typeof(MessageAttribute)).OfType<MessageAttribute>().First();
                return new
                {
                    MessageType = x,
                    customAttribute.Topic,
                    customAttribute.ProducerName,
                    ConcreteInterface = DefaultInterfaceType.MakeGenericType(x),
                    ConcreteType = DefaultType.MakeGenericType(x),
                };
            }).ToList();

        assemblies.Dump();

        foreach (var assembly in assemblies)
        {
            cluster.AddProducer(assembly.ProducerName, x => x.DefaultTopic(assembly.Topic));
        }

        return cluster;
    }

    public static IServiceCollection AddTypedProducers(this IServiceCollection services,
        params Type[] sourceTypes)
    {
        var assemblies = sourceTypes.Select(x => x.Assembly).Distinct().SelectMany(x => x.GetTypes())
            .Where(x => BaseMessage.IsAssignableFrom(x) && !x.IsAbstract).Select(x => new
            {
                MessageType = x,
                ConcreteInterface = DefaultInterfaceType.MakeGenericType(x),
                ConcreteType = DefaultType.MakeGenericType(x),
            }).ToList();

        assemblies.Dump();

        foreach (var assembly in assemblies)
        {
            services.AddSingleton(assembly.ConcreteInterface, assembly.ConcreteType);
        }

        return services;
    }
}