using System.Reflection;
using System.Text.Json;
using Dumpify;
using KafkaFlow;
using KafkaFlow.Admin.Messages;
using KafkaFlow.Configuration;
using KafkaFlow.Serializer;
using KafkaFlowExample.Attributes;
using KafkaFlowExample.Producers;
using Message = KafkaFlowExample.Messages.Message;

namespace KafkaFlowExample;

public static class Registration
{
    private static readonly Type DefaultInterfaceType = typeof(ITypedMessageProducer<>);
    private static readonly Type DefaultHandlerInterfaceType = typeof(IMessageHandler<>);
    private static readonly Type DefaultType = typeof(TypedMessageProducer<>);
    private static readonly Type BaseMessage = typeof(Message);

    public static IClusterConfigurationBuilder AddProducersAndConsumers(this IClusterConfigurationBuilder cluster,
        params Type[] sourceTypes)
    {
        var assemblies = sourceTypes.Select(x => x.Assembly).Distinct().SelectMany(x => x.GetTypes()).ToList();
        var producers = assemblies.Where(IsMessageType).Select(
            x =>
            {
                var customAttribute = GetInformation(x);
                return new
                {
                    MessageType = x,
                    customAttribute.Topic,
                    customAttribute.ProducerName,
                    ConcreteInterface = DefaultInterfaceType.MakeGenericType(x),
                    ConcreteType = DefaultType.MakeGenericType(x),
                };
            }).ToList();

        var consumers = assemblies.SelectMany(x => x.GetInterfaces().Where(IsAllowedInterfaceType).Select(y =>
        {
            var messageType = y.GetGenericArguments().First();
            var customAttribute = GetInformation(messageType);
            return new
            {
                MessageType = messageType,
                Topic = customAttribute.Topic,
                HandlerType = x
            };
        })).ToList();

        assemblies.Dump();

        foreach (var producer in producers)
        {
            cluster.AddProducer(producer.ProducerName, x =>
            {
                x.DefaultTopic(producer.Topic);
                x.AddMiddlewares(middlewares =>
                {
                    middlewares.AddSingleTypeSerializer<JsonCoreSerializer>(producer.MessageType);
                });
            });
        }

        foreach (var handler in consumers)
        {
            cluster.AddConsumer(consumer =>
            {
                consumer.Topic(handler.Topic);
                consumer.WithGroupId("group");
                consumer.WithBufferSize(1);
                consumer.AddMiddlewares(middleware =>
                {
                    middleware.AddTypedHandlers(handlers => handlers.AddHandlers([handler.HandlerType]));
                });
            });
        }

        return cluster;
    }

    private static bool IsAllowedType(Type type) => IsAllowedInterfaceType(type) || IsMessageType(type);

    private static bool IsAllowedInterfaceType(Type interfaceType)
    {
        return interfaceType.IsGenericType &&
               interfaceType.GetGenericTypeDefinition() == DefaultHandlerInterfaceType;
    }

    private static bool IsMessageType(Type type)
        => BaseMessage.IsAssignableFrom(type) && !type.IsAbstract;

    private static MessageAttribute GetInformation(Type type)
    {
        return type.GetCustomAttributes(typeof(MessageAttribute)).OfType<MessageAttribute>().First();
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

        foreach (var assembly in assemblies)
        {
            services.AddSingleton(assembly.ConcreteInterface, assembly.ConcreteType);
        }

        return services;
    }
}