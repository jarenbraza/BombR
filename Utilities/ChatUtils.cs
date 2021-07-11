using BombermanAspNet.Constants;
using BombermanAspNet.Extensions;
using BombermanAspNet.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BombermanAspNet.Utilities
{
	public class ChatUtils
	{
		private readonly IDistributedCache cache;

		public ChatUtils(IDistributedCache cache)
		{
			this.cache = cache;
		}

		public async Task<Chat> GetChat()
		{
			return (await cache.GetRecordAsync<Chat>(CacheConstants.ChatKey)) ?? new Chat();
		}

		public async Task SaveChat(Chat chat)
		{
			await cache.SetRecordAsync(CacheConstants.ChatKey, chat);
		}

		public async Task<ConnectionContext> PopConnectionContext(string connectionId)
		{
			ConnectionContext context = null;
			var chat = await GetChat();

			if (chat != null && chat.ConnectionContexts.ContainsKey(connectionId))
			{
				context = chat.ConnectionContexts[connectionId];
				chat.ConnectionContexts.Remove(connectionId);
				await SaveChat(chat);
			}

			return context;
		}

		public async Task AddConnectionContext(string connectionId, ConnectionContext context)
		{
			var chat = await GetChat();
			
			if (chat != null)
			{
				chat.ConnectionContexts[connectionId] = context;
			}

			await SaveChat(chat);
		}

		public async Task<List<string>> GetPlayerNamesInRoom(string roomName)
		{
			var playerNames = new List<string>();
			var chat = await GetChat();

			if (chat != null)
			{
				foreach (var context in chat.ConnectionContexts.Values)
				{
					if (roomName.Equals(context.RoomName))
					{
						playerNames.Add(context.PlayerName);
					}
				}
			}

			return playerNames;
		}
	}
}
