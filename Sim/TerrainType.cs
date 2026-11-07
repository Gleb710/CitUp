namespace CitUp.Sim;

/// <summary>Тип природной клетки карты.</summary>
public enum TerrainType
{
    /// <summary>Ровная проходимая земля, пригодная для строительства.</summary>
    Plain,

    /// <summary>Лес. Проходим, но движение стоит в 3 раза дороже. Строить нельзя.</summary>
    Forest,

    /// <summary>Переход высоты. Проходим в обоих направлениях.</summary>
    Slope,

    /// <summary>Непроходимый обрыв или скала.</summary>
    Cliff,

    /// <summary>Скала с ресурсным вкраплением. Непроходима, но может быть источником камня.</summary>
    ResourceCliff,

    /// <summary>Вода. Непроходима для зданий и юнитов.</summary>
    Water
}