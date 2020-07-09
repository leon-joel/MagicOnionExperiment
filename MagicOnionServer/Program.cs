using System;
using Grpc.Core;
using MagicOnion.Server;

namespace MagicOnionServer
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Server startup!");

			//--- ここで動的にアセンブリを解釈し、通信されてきたデータと API 本体とのマップを作る
			var service = MagicOnionEngine.BuildServerServiceDefinition();

			//--- API を公開する IP / Port / 認証情報などを決定し、gRPC サーバーを起動
			var port = new ServerPort("localhost", 12345, ServerCredentials.Insecure);
			var server = new Server(new[] {
				new ChannelOption(ChannelOptions.MaxReceiveMessageLength, int.MaxValue),
				new ChannelOption(ChannelOptions.MaxSendMessageLength, int.MaxValue),
			}) {
				Services = { service },
				Ports = { port },
			};
			server.Start();

			//--- exe が終了しちゃわないように
			Console.ReadLine();
		}
	}
}
