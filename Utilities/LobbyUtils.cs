using BombermanAspNet.Constants;
using BombermanAspNet.Extensions;
using BombermanAspNet.Hubs;
using BombermanAspNet.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BombermanAspNet.Utilities
{
	public class LobbyUtils
    {
        private readonly IDistributedCache cache;
        private readonly object lobbyLock = new();
        private readonly IHubContext<LobbyHub> lobbyHub;

        public LobbyUtils(IDistributedCache cache, IHubContext<LobbyHub> lobbyHub)
		{
            this.cache = cache;
            this.lobbyHub = lobbyHub;
        }

        public async Task<Lobby> GetLobby()
        {
            return (await cache.GetRecordAsync<Lobby>(CacheConstants.LobbyKey).ConfigureAwait(false)) ?? new Lobby();
        }

        public async Task SaveLobby(Lobby lobby)
        {
            await cache.SetRecordAsync(CacheConstants.LobbyKey, lobby).ConfigureAwait(false);
        }

        public async Task UpdateLobbyForRoom(string roomName)
        {
            var room = await GetRoom(roomName).ConfigureAwait(false);
            await lobbyHub.Clients.All.SendAsync("UpdateRoomInTable", room).ConfigureAwait(false);
        }

        public async Task AddConnectionContext(string connectionId, ConnectionContext context)
        {
            var lobby = await GetLobby().ConfigureAwait(false);

            lock (lobbyLock)
            {
                lobby.ConnectionContexts.Add(connectionId, context);
            }

            await SaveLobby(lobby).ConfigureAwait(false);
        }

        public async Task<ConnectionContext> PopConnectionContext(string connectionId)
        {
            ConnectionContext context = null;
            var lobby = await GetLobby().ConfigureAwait(false);

            lock (lobbyLock)
            {
                lobby.ConnectionContexts.Remove(connectionId, out context);
            }

            await SaveLobby(lobby).ConfigureAwait(false);
            return context;
        }

        public async Task<List<ConnectionContext>> GetConnectionContexts()
        {
            var lobby = await GetLobby().ConfigureAwait(false);
            return new(lobby.ConnectionContexts.Values);
        }

        public async Task<ConnectionContext> GetConnectionContext(string connectionId)
        {
            var lobby = await GetLobby().ConfigureAwait(false);

            if (lobby == null || !lobby.ConnectionContexts.ContainsKey(connectionId))
			{
                return null;
			}

            return lobby.ConnectionContexts[connectionId];
        }

        public async Task<List<string>> GetRoomNames()
		{
            var roomNames = new HashSet<string>();

            foreach (var context in await GetConnectionContexts().ConfigureAwait(false))
            {
                roomNames.Add(context.RoomName);
            }

            return new(roomNames);
        }

        public async Task<List<string>> GetPlayerNames()
        {
            var playerNames = new HashSet<string>();

            foreach (var context in await GetConnectionContexts().ConfigureAwait(false))
            {
                playerNames.Add(context.PlayerName);
            }

            return new(playerNames);
        }

        public async Task<List<string>> GetPlayerNamesInRoom(string roomName)
        {
            var playerNames = new HashSet<string>();

            foreach (var context in await GetConnectionContexts().ConfigureAwait(false))
            {
                if (roomName.Equals(context.RoomName))
                {
                    playerNames.Add(context.PlayerName);
                }
            }

            return new(playerNames);
        }

        // TODO: Gross. Surely there's a better way to expose which players are in a room.
        public async Task<List<Room>> GetRooms()
        {
            var roomDictionary = new Dictionary<string, List<string>>();

            foreach (var context in await GetConnectionContexts().ConfigureAwait(false))
            {
                if (!roomDictionary.ContainsKey(context.RoomName))
                {
                    roomDictionary[context.RoomName] = new List<string>();
                }

                roomDictionary[context.RoomName].Add(context.PlayerName);
            }

            var rooms = new List<Room>();

            foreach (var roomName in roomDictionary.Keys)
			{
                rooms.Add(new Room()
                {
                    RoomName = roomName,
                    PlayerNames = new(roomDictionary[roomName])
                });
			}

            return rooms;
        }

        public async Task<Room> GetRoom(string roomName)
        {
            var playerNames = await GetPlayerNamesInRoom(roomName);
            return new Room()
            {
                RoomName = roomName,
                PlayerNames = playerNames
            };
        }
    }
}
