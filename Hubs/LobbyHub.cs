using BombermanAspNet.Utilities;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BombermanAspNet.Hubs
{
	public class LobbyHub : Hub
    {
        private readonly LobbyUtils lobby;

        public LobbyHub(LobbyUtils lobby)
        {
            this.lobby = lobby;
        }

        public async override Task OnConnectedAsync()
        {
            foreach (var room in await lobby.GetRooms())
            {
                await Clients.Caller.SendAsync("UpdateRoomInTable", room);
            }

            await base.OnConnectedAsync();
        }

        // Join is valid if player is not yet in the room
        public async Task<bool> ValidateJoin(string roomName, string playerName)
		{
            var playerNames = await lobby.GetPlayerNamesInRoom(roomName);
            return !playerNames.Contains(playerName);
        }

        public async Task<string> GenerateRoomName(GenerateRoomNameRequest request)
        {
            if (string.IsNullOrEmpty(request.PlayerName))
            {
                throw new ArgumentException(nameof(request.PlayerName));
            }

            var roomName = RoomNameGenerator.GenerateUniqueRoomName(await lobby.GetRoomNames());

            return roomName;
        }

        // Securely generates random room names from a preset character set and length.
        private class RoomNameGenerator
        {
            private const string ValidCharacters = "ABCDEFGHJKLMNPQRTUVWXYZ2346789";
            private const int RoomNameLength = 5;
            private const int MaxRetries = 1000;

            public static string GenerateUniqueRoomName(ICollection<string> roomNames)
            {
                string roomName;
                int retries = 0;

				do
				{
					roomName = GenerateRoomName();
					retries++;
				} while (roomNames.Contains(roomName) && retries < MaxRetries);

				return roomName;
            }

            private static string GenerateRoomName()
            {
                StringBuilder stringBuilder = new();

                using (var rng = new RNGCryptoServiceProvider())
                {
                    byte[] buffer = new byte[sizeof(uint)];

                    for (int i = 0; i < RoomNameLength; i++)
                    {
                        rng.GetBytes(buffer);
                        uint randomValue = BitConverter.ToUInt32(buffer);
                        char randomValidCharacter = ValidCharacters[(int)(randomValue % (uint)ValidCharacters.Length)];
                        stringBuilder.Append(randomValidCharacter);
                    }
                }

                return stringBuilder.ToString();
            }
        }
    }

    public class GenerateRoomNameRequest
    {
        public string PlayerName { get; set; }
    }
}
