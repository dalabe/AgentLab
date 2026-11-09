using System.Collections.Concurrent;
using Microsoft.Extensions.AI;

namespace Procesos.ChatApi.Sessions;

/// <summary>
/// Historial de conversación en memoria, por sessionId. No hay autenticación de usuario en este
/// laboratorio, así que el sessionId (generado por el cliente o por este servicio) es el único
/// aislamiento entre conversaciones. Se pierde al reiniciar el proceso: suficiente para un lab.
/// </summary>
public sealed class ChatSessionStore
{
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _sessions = new();

    public List<ChatMessage> GetOrCreate(string sessionId) => _sessions.GetOrAdd(sessionId, static _ => []);
}
