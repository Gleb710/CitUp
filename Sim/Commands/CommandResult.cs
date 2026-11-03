namespace CitUp.Sim;

/// <summary>Итог проверки команды. Error == None значит, что команда принята.</summary>
public sealed record CommandResult(int CommandId, CommandError Error)
{
    public bool Ok => Error == CommandError.None;

    /// <summary>Текст для показа игроку.</summary>
    public string Message => Error switch
    {
        CommandError.None => "Готово",
        CommandError.OutOfBounds => "Клетка вне карты",
        CommandError.UnknownType => "Неизвестный тип здания",
        CommandError.CellOccupied => "Клетка занята",
        CommandError.CellBlockedByResident => "Клетка на пути жителя",
        _ => "Неизвестная ошибка"
    };
}