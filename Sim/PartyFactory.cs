namespace CitUp.Sim;

/// <summary>Создаёт новую партию с детерминированно сгенерированной картой.</summary>
public static class PartyFactory
{
    public static World CreateNewParty(GameDataCatalog gameData, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(gameData);

        var options = gameData.MapGeneration.ToOptions(seed);
        var terrain = MapGenerator.Generate(options);

        var worldConfig = WorldConfig.LoadDefault();

        if (worldConfig.Map.Width != options.Width ||
            worldConfig.Map.Height != options.Height)
        {
            throw new InvalidDataException(
                $"Размер карты в WorldConfig {worldConfig.Map.Width}×{worldConfig.Map.Height} " +
                $"не совпадает с размером из настроек генерации {options.Width}×{options.Height}.");
        }

        var world = new World(worldConfig, terrain);

        var playerStockpile = world.GetPlayerStockpile(1);

        if (gameData.Startup.StartingMoney > 0)
            playerStockpile.AddMoney(gameData.Startup.StartingMoney);

        foreach (var resource in gameData.Startup.StartingResources)
        {
            if (resource.Amount > 0)
                playerStockpile.AddResource(resource.ResourceId, resource.Amount);
        }

        return world;
    }
}