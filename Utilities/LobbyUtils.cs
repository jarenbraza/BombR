using BombermanAspNet.Constants;
using BombermanAspNet.Extensions;
using BombermanAspNet.Hubs;
using BombermanAspNet.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BombermanAspNet.Utilities
{
    public class LobbyUtils
    {
        private readonly IDistributedCache cache;
        private readonly IHubContext<LobbyHub> lobbyHub;
        private readonly SemaphoreSlim lobbyLock = new(1, 1);

        public LobbyUtils(IDistributedCache cache, IHubContext<LobbyHub> lobbyHub)
        {
            this.cache = cache;
            this.lobbyHub = lobbyHub;
        }

        public async Task<Lobby> GetLobby()
        {
            return await cache.GetRecordAsync<Lobby>(CacheConstants.LobbyKey);
        }

        public async Task SaveLobby(Lobby lobby)
        {
            await cache.SetRecordAsync(CacheConstants.LobbyKey, lobby);
        }

        public async Task UpdateLobbyForRoom(string roomName)
        {
            var room = await GetRoom(roomName);
            await lobbyHub.Clients.All.SendAsync("UpdateRoomInTable", room);
        }

        public async Task AddConnectionContext(string connectionId, ConnectionContext context)
        {
            await lobbyLock.WaitAsync();

            try
            {
                var lobby = await GetLobby();

                if (lobby == null)
                {
                    lobby = new Lobby();
                }

                lobby.ConnectionContexts.Add(connectionId, context);

                await SaveLobby(lobby);
            }
            finally
            {
                lobbyLock.Release();
            }
        }

        public async Task<ConnectionContext> PopConnectionContext(string connectionId)
        {
            await lobbyLock.WaitAsync();

            try
            {
                ConnectionContext context = null;
                var lobby = await GetLobby();

                if (lobby != null)
                {
                    lobby.ConnectionContexts.Remove(connectionId, out context);
                    await SaveLobby(lobby);
                }

                return context;
            }
            finally
            {
                lobbyLock.Release();
            }
        }

        public async Task<List<ConnectionContext>> GetConnectionContexts()
        {
            await lobbyLock.WaitAsync();

            try
            {
                var lobby = await GetLobby();

                return (lobby == null) ? new() : new(lobby.ConnectionContexts.Values);
            }
            finally
            {
                lobbyLock.Release();
            }
        }

        public async Task<List<string>> GetRoomNames()
        {
            var roomNames = new HashSet<string>();

            foreach (var context in await GetConnectionContexts())
            {
                roomNames.Add(context.RoomName);
            }

            return new(roomNames);
        }

        public async Task<List<string>> GetPlayerNamesInRoom(string roomName)
        {
            var playerNames = new HashSet<string>();

            foreach (var context in await GetConnectionContexts())
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

            foreach (var context in await GetConnectionContexts())
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
