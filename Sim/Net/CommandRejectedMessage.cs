namespace CitUp.Sim;

/// <summary>Хост → автору команды: команда отклонена. Текст для экрана клиент берёт из CommandResult.Message.</summary>
public sealed record CommandRejectedMessage(int CommandId, CommandError Error) : NetMessage;