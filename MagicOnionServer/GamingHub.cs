using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MagicOnion.Server;
using MagicOnion.Server.Hubs;
using MagicOnionExperiment.ServiceDefinitions;

namespace MagicOnionServer
{
	class GamingHub : StreamingHubBase<IGamingHub, IGamingHubReceiver>, IGamingHub
	{
		Player _player;
		IGroup _room;
		IInMemoryStorage<Player> _storage;

		public async Task<Player[]> JoinAsync(string roomName, string userName)
		{
			_player = new Player() { Name = userName };
			(_room, _storage) = await Group.AddAsync(roomName, _player);

			Console.WriteLine($"player [{userName}] joined to room [{roomName}].");

			Broadcast(_room).OnJoin(_player);
			return _storage.AllValues.ToArray();
		}

		public async Task LeaveAsync()
		{
			await _room.RemoveAsync(this.Context);

			Console.WriteLine($"player [{_player.Name}] left from room [{_room.GroupName}].");

			Broadcast(_room).OnLeave(_player);
		}
	}
}
