// ReSharper disable InconsistentNaming
using System.Collections.Generic;
using EasyNetQ.Serialization.NewtonsoftJson;
using EasyNetQ.Serialization.SystemTextJson;
using FluentAssertions;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Serialization.Tests;

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters

public class SerializerTests
{
    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_and_deserialize_a_message(string name, ISerializer serializer)
    {
        var message = new Message { Text = "Hello World" };

        using var serializedMessage = serializer.MessageToBytes(typeof(Message), message);
        var deserializedMessage = (Message)serializer.BytesToMessage(typeof(Message), serializedMessage.Memory);

        message.Text.Should().Be(deserializedMessage.Text);
    }

    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_basic_properties(string name, ISerializer serializer)
    {
        var originalProperties = new EasyNetQ.Tests.BasicProperties
        {
            AppId = "some app id",
            ClusterId = "cluster id",
            ContentEncoding = "content encoding",
            ContentType = "content type",
            CorrelationId = "correlation id",
            DeliveryMode = 4,
            Expiration = "1",
            MessageId = "message id",
            Priority = 1,
            ReplyTo = "abc",
            Timestamp = new AmqpTimestamp(123344044),
            Type = "Type",
            UserId = "user id",
            Headers = new Dictionary<string, object>
            {
                { "one", "header one" },
                { "two", "header two" }
            }
        };

        var messageBasicProperties = new MessageProperties();
        messageBasicProperties.CopyFrom(originalProperties);
        using var serializedMessage = serializer.MessageToBytes(typeof(MessageProperties), messageBasicProperties);
        var deserializedMessageBasicProperties = (MessageProperties)serializer.BytesToMessage(
            typeof(MessageProperties), serializedMessage.Memory
        );

        var newProperties = new EasyNetQ.Tests.BasicProperties();
        deserializedMessageBasicProperties.CopyTo(newProperties);

        originalProperties.Should().BeEquivalentTo(newProperties);
    }

    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_and_deserialize_polymorphic_properties(string name, ISerializer serializer)
    {
        if (name == "SystemTextJson") return; // Polymorphic deserialization doesn't work out of the box

        using var serializedMessage = serializer.MessageToBytes(typeof(PolyMessage), new PolyMessage { AorB = new B() });
        var result = (PolyMessage)serializer.BytesToMessage(typeof(PolyMessage), serializedMessage.Memory);
        Assert.IsType<B>(result.AorB);
    }

    public static IEnumerable<object[]> GetSerializers()
    {
        yield return new object[] { "Newtonsoft", new NewtonsoftJsonSerializer() };
        yield return new object[] { "Default", new JsonSerializer() };
        yield return new object[] { "SystemTextJson", new SystemTextJsonSerializer() };
    }


    private class A { }
    private class B : A { }
    private class PolyMessage
    {
        public A AorB { get; set; }
    }
    private class Message
    {
        public string Text { get; set; }
    }
}

// ReSharper restore InconsistentNaming
