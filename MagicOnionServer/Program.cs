using System;
using Grpc.Core;
using Grpc.Core.Logging;
using MagicOnion.Server;

namespace MagicOnionServer
{
	class Program
	{
		static void Main(string[] args)
		{
			// GrpcのLogging機構を使う
			GrpcEnvironment.SetLogger(new ConsoleLogger());
			GrpcEnvironment.Logger.Info("Server startup! =================================");

			// MaginOnion側のログを gRPC のログに流し込む設定
			// ※JSON形式でデータダンプする
			var logger = new MagicOnionLogToGrpcLoggerWithDataDump();
			var options = new MagicOnionOptions(true) { MagicOnionLogger = logger };

			//--- ここで動的にアセンブリを解釈し、通信されてきたデータと API 本体とのマップを作る
			var service = MagicOnionEngine.BuildServerServiceDefinition(options);

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

			GrpcEnvironment.Logger.Info("Server started");

			//--- exe が終了しちゃわないように
			Console.ReadLine();

			GrpcEnvironment.Logger.Info("Server closed. ---------------------------------");
		}
	}
}
