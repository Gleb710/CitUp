namespace CitUp.Sim;

/// <summary>Хост → клиент: подключение отклонено, причина готова к показу игроку.</summary>
public sealed record JoinRejectedMessage(string Reason) : NetMessage;