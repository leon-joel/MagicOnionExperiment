using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MagicOnion;
using MagicOnion.Server;
using MagicOnionExperiment.ServiceDefinitions;
using MessagePack;

namespace MagicOnionServer
{
	class ConnectionIDService : ServiceBase<IConnectionIDService>, IConnectionIDService
	{
		public async UnaryResult<Nil> SendConnectionID()
		{
			//--- こんな感じで取り出せます
			// ★2020/08/05現在(MagicOnion3.0.13では) GetConnectionContext() というメソッドは存在しない！廃止された？！
			//var connectionId = this.GetConnectionContext().ConnectionId;
			//return Nil.Default;

			// ★仕方ないので、自前でヘッダーから取り出す
			var header = this.Context.CallContext.RequestHeaders;
			var contextID = header.Get("ConnectionId").Value;
			var contextIDJa = header.Get("ConnectionId_ja").Value;

			Console.WriteLine($"ConnectionId: {contextID}");
			Console.WriteLine($"ConnectionId_ja: {contextIDJa}");	// 日本語は文字化けするっぽい
			return Nil.Default;
		}
	}
}
