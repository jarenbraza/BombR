using BombermanAspNet.Data;
using BombermanAspNet.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BombermanAspNet.Hubs
{
    public class LobbyHub : Hub
    {
        private readonly Lobby lobby;

        public LobbyHub(Lobby lobby)
        {
            this.lobby = lobby;
        }

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            if (!lobby.TryRemoveConnectionContext(Context.ConnectionId, out var connectionContext))
            {
                Debug.WriteLine("Unable to get context for ID " + Context.ConnectionId);
                return;
            }

            var playerName = connectionContext.PlayerName;
            var roomName = connectionContext.RoomName;

            await Clients.Group(roomName).SendAsync("PlayerDisconnected", playerName);

            if (!lobby.TryRemovePlayerFromRoom(playerName, roomName, out var playerNames))
            {
                throw new HubException("Unable to remove player " + playerName + " from room " + roomName);
            }

            var roomModel = new RoomModel(roomName, playerNames);
            await Clients.All.SendAsync("UpdateRoom", roomModel).ConfigureAwait(false);
            lobby.TryRemoveEmptyRoom(roomName);
        }

        public async override Task OnConnectedAsync()
        {
            foreach (var roomName in lobby.GetRooms())
            {
                if (lobby.TryGetPlayerNames(roomName, out var playerNames)) {
                    RoomModel roomModel = new(roomName, playerNames);
                    await Clients.Caller.SendAsync("UpdateRoom", roomModel).ConfigureAwait(false);
                }
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinLobbyRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task AddPlayerToRoom(AddPlayerToRoomRequest request)
        {
            var playerName = request.PlayerName;
            var roomName = request.RoomName;

            if (string.IsNullOrEmpty(playerName))
            {
                throw new ArgumentException(nameof(playerName));
            }

            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            if (lobby.TryAddPlayerToRoom(playerName, roomName))
            {
                if (lobby.TryGetPlayerNames(roomName, out var playerNames))
                {
                    RoomModel roomModel = new(roomName, playerNames);
                    await Clients.All.SendAsync("UpdateRoom", roomModel).ConfigureAwait(false);
                    await Clients.Caller.SendAsync("RedirectToRoom", roomName, playerName).ConfigureAwait(false);
                }
                else
                {
                    throw new HubException("Failed to get information for room " + roomName);
                }
            }
            else
            {
                throw new HubException("Failed to add player " + playerName + " to room " + roomName);
            }
        }

        public string CreateRoom(CreateRoomRequest request)
        {
            if (string.IsNullOrEmpty(request.PlayerName))
            {
                throw new ArgumentException(nameof(request.PlayerName));
            }

            var roomName = RoomNameGenerator.GenerateUniqueRoomName(lobby);

            if (!lobby.TryAddRoom(roomName))
            {
                throw new HubException("Failed to create a room with room name " + roomName);
            }

            return roomName;
        }

        public void RegisterConnection(ConnectionContext model)
        {
            if (!lobby.TryAddConnectionContext(Context.ConnectionId, model))
            {
                throw new HubException("Context for ID " + Context.ConnectionId + " already exists");
            }
        }

        // Securely generates random room names from a preset character set and length.
        private class RoomNameGenerator
        {
            private const string VALID_CHARACTERS = "ABCDEFGHJKLMNPQRTUVWXYZ2346789";
            private const int ROOM_NAME_LENGTH = 5;

            public static string GenerateUniqueRoomName(Lobby lobbyDetails)
            {
                string roomName;

                do
                {
                    roomName = GenerateRoomName();
                } while (lobbyDetails.TryGetPlayerNames(roomName, out _));

                return roomName;
            }

            private static string GenerateRoomName()
            {
                StringBuilder stringBuilder = new StringBuilder();

                using (var rng = new RNGCryptoServiceProvider())
                {
                    byte[] buffer = new byte[sizeof(uint)];

                    for (int i = 0; i < ROOM_NAME_LENGTH; i++)
                    {
                        rng.GetBytes(buffer);
                        uint randomValue = BitConverter.ToUInt32(buffer);
                        char randomValidCharacter = VALID_CHARACTERS[(int)(randomValue % (uint)VALID_CHARACTERS.Length)];
                        stringBuilder.Append(randomValidCharacter);
                    }
                }

                return stringBuilder.ToString();
            }
        }
    }

    public class AddPlayerToRoomRequest
    {
        public string PlayerName { get; set; }
        public string RoomName { get; set; }
    }

    public class CreateRoomRequest
    {
        public string PlayerName { get; set; }
    }
}
