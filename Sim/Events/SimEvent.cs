using System.Text.Json.Serialization;

namespace CitUp.Sim;

/// <summary>
/// Базовый тип события. Любое изменение мира описывается событием.
/// Атрибуты нужны сериализатору: по строке в поле $type он узнаёт, какое именно событие пришло.
/// Новое событие = новый класс + новая строка здесь.
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(BuildingBuilt), "buildingBuilt")]
[JsonDerivedType(typeof(ResidentWalkStarted), "residentWalkStarted")]
[JsonDerivedType(typeof(ResidentWalkFinished), "residentWalkFinished")]
public abstract record SimEvent;