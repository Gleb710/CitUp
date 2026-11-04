namespace CitUp.Sim;

/// <summary>Хост → клиент: ответ на Ping. Клиент вычитает ClientTimeMs из текущего времени и получает задержку.</summary>
public sealed record PongMessage(long ClientTimeMs) : NetMessage;