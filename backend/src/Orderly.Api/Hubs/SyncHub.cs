using Microsoft.AspNetCore.SignalR;

namespace Orderly.Api.Hubs;

/// <summary>Hub de sincronização em tempo real entre clientes. Métodos entram na Fase 1.</summary>
public sealed class SyncHub : Hub;
