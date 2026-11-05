namespace CitUp.Sim;

/// <summary>Клиент → хост: мой мир разошёлся с вашим, пришлите снимок заново.</summary>
public sealed record ResyncRequestMessage : NetMessage;