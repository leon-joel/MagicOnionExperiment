using System;
using System.Threading.Tasks;
using MagicOnion;
using MessagePack;

namespace MagicOnionExperiment.ServiceDefinitions
{
	public interface ISampleApi : IService<ISampleApi>
	{
		// Unary 通信
		UnaryResult<int> Sum(int x, int y);

		// Server Streaming 通信
		Task<ServerStreamingResult<int>> Repeat(int value, int count);

		// Client Streaming 通信 ※引数は与えられないとのこと (gRPCの制限)
		Task<ClientStreamingResult<int, int>> SplitUpload();

		// Duplex Streaming 通信 ※引数は与えられない
		Task<DuplexStreamingResult<int, int>> DuplexSample();


		// 独自型を送信
		UnaryResult<Nil> Sample(Vector2 point);
	}
}
