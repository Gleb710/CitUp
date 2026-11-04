namespace CitUp.Sim;

/// <summary>Хост → клиент: назначенный PlayerId и полный снимок мира.</summary>
public sealed record WelcomeMessage(int PlayerId, WorldSnapshot Snapshot) : NetMessage;