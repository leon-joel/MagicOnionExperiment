using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using MagicOnion.Client;
using MagicOnionExperiment.ServiceDefinitions;

namespace MagicOnionExperiment.Client
{
	public struct GameObject
	{

	}

	public class GamingHubClient : IGamingHubReceiver
	{
		Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

		// 委譲したメソッドを立てるのが面倒な場合は（面倒）これをそのまま公開したりしても勿論別に良い。
		IGamingHub client;

		public async Task<GameObject> ConnectAsync(Channel grpcChannel, string roomName, string playerName)
		{
			var client = StreamingHubClient.Connect<IGamingHub, IGamingHubReceiver>(grpcChannel, this);

			var roomPlayers = await client.JoinAsync(roomName, playerName);

			// なぜサーバーからではなくクライアントから自前でみんなに通知しているのか分からないのでコメント化
			//foreach (var player in roomPlayers) {
			//	(this as IGamingHubReceiver).OnJoin(player);
			//}

			return players[playerName]; // 名前だけでマッチとか脆弱の極みですが、まぁサンプルなので。
		}

		// サーバーへ送るメソッド群

		public Task LeaveAsync()
		{
			return client.LeaveAsync();
		}

		// 後始末するもの
		public Task DisposeAsync()
		{
			return client.DisposeAsync();
		}

		// 正常/異常終了を監視できる。これを待ってリトライかけたりなど。
		public Task WaitForDisconnect()
		{
			return client.WaitForDisconnect();
		}

		// サーバーからBroadcastされたものを受信するメソッド

		void IGamingHubReceiver.OnJoin(Player player)
		{
			Console.WriteLine("Join Player:" + player.Name);

			var cube = new GameObject();
			players[player.Name] = cube;
		}

		void IGamingHubReceiver.OnLeave(Player player)
		{
			Console.WriteLine("Leave Player:" + player.Name);

			if (players.TryGetValue(player.Name, out var cube)) {
				//GameObject.Destroy(cube);
			}
		}
	}
}
