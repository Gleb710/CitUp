namespace CitUp.Sim;

public enum CommandError
{
    None,
    OutOfBounds,
    UnknownType,
    CellOccupied,
    CellBlockedByResident,
    UnknownBuilding,
    InvalidAmount,
    ClearanceBlocked,
    InvalidTerrain,
    UnevenTerrain,
    InvalidRotation
}