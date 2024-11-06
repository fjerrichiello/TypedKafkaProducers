using System.Reflection;
using Dumpify;
using Microsoft.OpenApi.Models;
using KafkaFlow;
using KafkaFlow.Producers;
using KafkaFlowExample;
using KafkaFlowExample.Attributes;
using KafkaFlowExample.Handlers;
using KafkaFlowExample.Messages;
using KafkaFlowExample.Producers;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();


services
    .AddKafka(
        kafka => kafka
            .UseConsoleLog()
            .AddCluster(
                cluster => cluster
                    .WithBrokers(new[] { "localhost:9092" })
                    .CreateTopicIfNotExists(nameof(TestMessageV1), 6, 1)
                    .EnableAdminMessages("kafkaflow.admin")
                    .AddProducers(typeof(TestMessageV1))
                    .AddConsumer(consumer =>
                    {
                        consumer.Topic(nameof(TestMessageV1));
                        consumer.WithGroupId("group");
                        consumer.WithBufferSize(1);
                        consumer.AddMiddlewares(middleware =>
                        {
                            middleware.AddTypedHandlers(handlers => handlers.AddHandler<TextMessageHandler>());
                        });
                    })
            )
    );


services
    .AddSwaggerGen(
        c =>
        {
            c.SwaggerDoc(
                "kafkaflow",
                new OpenApiInfo
                {
                    Title = "KafkaFlow Admin",
                    Version = "kafkaflow",
                });
        })
    .AddControllers();

services.AddTypedProducers(typeof(TestMessageV1));
var app = builder.Build();

app.MapPost("/produce",
    async (ITypedMessageProducer<TestMessageV1> _producer, string text) =>
    {
        await _producer.ProduceAsync(new TestMessageV1("Key", "Text"));
        return Results.Ok();
    });
app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/kafkaflow/swagger.json", "KafkaFlow Admin"); });

var kafkaBus = app.Services.CreateKafkaBus();
await kafkaBus.StartAsync();

app.Run();