using System.Text.Json.Serialization;

namespace CitUp.Sim;

/// <summary>Базовый тип всех сообщений между хостом и клиентом.</summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(HelloMessage), "hello")]
[JsonDerivedType(typeof(WelcomeMessage), "welcome")]
[JsonDerivedType(typeof(CommandMessage), "command")]
[JsonDerivedType(typeof(CommandRejectedMessage), "commandRejected")]
[JsonDerivedType(typeof(EventsMessage), "events")]
[JsonDerivedType(typeof(HeartbeatMessage), "heartbeat")]
[JsonDerivedType(typeof(PingMessage), "ping")]
[JsonDerivedType(typeof(PongMessage), "pong")]
public abstract record NetMessage;