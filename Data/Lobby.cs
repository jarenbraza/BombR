using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BombermanAspNet.Data
{
    public class Lobby
    {
        // Associates connections to room data.
        private readonly ConcurrentDictionary<string, ConnectionContext> connectionContexts = new();

        // Associates rooms to connected players.
        private readonly ConcurrentDictionary<string, List<string>> playerNamesOfRoom = new();

        public bool TryAddConnectionContext(string connectionId, ConnectionContext model)
        {
            if (connectionId is null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return connectionContexts.TryAdd(connectionId, model);
        }

        public bool TryRemoveConnectionContext(string connectionId, out ConnectionContext model)
        {
            if (connectionId is null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            return connectionContexts.TryRemove(connectionId, out model);
        }

        public ICollection<string> GetRooms()
        {
            return playerNamesOfRoom.Keys;
        }

        public bool TryGetPlayerNames(string roomName, out List<string> playerNames)
        {
            if (roomName is null)
            {
                throw new ArgumentNullException(nameof(roomName));
            }

            return playerNamesOfRoom.TryGetValue(roomName, out playerNames);
        }

        public bool TryAddRoom(string roomName)
        {
            if (roomName is null)
            {
                throw new ArgumentNullException(nameof(roomName));
            }

            return playerNamesOfRoom.TryAdd(roomName, new List<string>());
        }

        public bool TryAddPlayerToRoom(string playerName, string roomName)
        {
            if (playerName is null)
            {
                throw new ArgumentNullException(nameof(playerName));
            }

            if (roomName is null)
            {
                throw new ArgumentNullException(nameof(roomName));
            }

            if (playerNamesOfRoom.TryGetValue(roomName, out var initialPlayerNames))
            {
                var updatedPlayerNames = new List<string>(initialPlayerNames);
                updatedPlayerNames.Add(playerName);
                return playerNamesOfRoom.TryUpdate(roomName, updatedPlayerNames, initialPlayerNames);
            }

            return false;
        }

        public bool TryRemovePlayerFromRoom(string playerName, string roomName, out List<string> updatedPlayerNames)
        {
            if (playerName is null)
            {
                throw new ArgumentNullException(nameof(playerName));
            }

            if (roomName is null)
            {
                throw new ArgumentNullException(nameof(roomName));
            }

            updatedPlayerNames = new();

            if (playerNamesOfRoom.TryGetValue(roomName, out var initialPlayerNames))
            {
                updatedPlayerNames.AddRange(initialPlayerNames);
                updatedPlayerNames.Remove(playerName);
                return playerNamesOfRoom.TryUpdate(roomName, updatedPlayerNames, initialPlayerNames);
            }

            return false;
        }

        public bool TryRemoveEmptyRoom(string roomName)
        {
            if (roomName is null)
            {
                throw new ArgumentNullException(nameof(roomName));
            }

            // Race condition, but should be handled when player attempts to join a non-existent room.
            if (playerNamesOfRoom.TryGetValue(roomName, out var playerNames) && playerNames.Count == 0)
            {
                return playerNamesOfRoom.TryRemove(roomName, out _);
            }

            return false;
        }
    }
}
