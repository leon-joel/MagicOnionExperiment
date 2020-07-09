using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Server;
using MagicOnionExperiment.ServiceDefinitions;
using MessagePack;

namespace MagicOnionServer
{
	class SampleApi : ServiceBase<ISampleApi>, ISampleApi
	{
		// MagicOnion入門(4): Unary通信
		// https://blog.xin9le.net/entry/2017/06/13/130025
		//--- async/await をそのまま書ける
		public async UnaryResult<int> Sum(int x, int y)
		{
			Logger.Debug($"(x, y) = ({x}, {y})");
			await Task.Delay(10);  // 何か非同期処理したり
			return x + y;
		}

		/*
		//--- 「await がないぞ！」と警告されるけれど、これでも OK
		//--- プロジェクトレベルで警告 CS1998 を抑制するのがオススメです
		public async UnaryResult<int> Sum(int x, int y)
			=> x + y;

		//--- async を使わない書き方もできるけれどスマートじゃないので not recommend
		public UnaryResult<int> Sum(int x, int y)
			=> new UnaryResult<int>(x + y);
		*/

		// Server Streaming
		public async Task<ServerStreamingResult<int>> Repeat(int value, int count)
		{
			Logger.Debug($"(value, count) = ({value}, {count})");

			// --- WriteAsyncするたびにレスポンスが返る
			var streaming = this.GetServerStreamingContext<int>();
			foreach (var x in Enumerable.Repeat(value, count)) {
				await streaming.WriteAsync(x);
			}

			// --- 完了信号を返す
			return streaming.Result();
		}

		// Client Streaming
		public async Task<ClientStreamingResult<int, int>> SplitUpload()
		{
			//--- クライアント側が WriteAsync するたびに呼び出される
			//--- CompleteAsync されるまでメッセージを受信し続ける
			var streaming = this.GetClientStreamingContext<int, int>();
			var sum = 0;
			await streaming.ForEachAsync(x => {
				Logger.Debug($"Received = {x}");
				sum += x;
			});

			//--- 結果を返す
			return streaming.Result(sum);
		}

		// Duplex Streaming
		public async Task<DuplexStreamingResult<int, int>> DuplexSample()
		{
			var streaming = this.GetDuplexStreamingContext<int, int>();
			var task = streaming.ForEachAsync(async x => {
				//--- クライアントから送信された値が偶数だったら 2 倍にして返してみたり
				Logger.Debug($"[Dup] Received : {x}");
				if (x % 2 == 0)
					await streaming.WriteAsync(x * 2);
			});

			//--- サーバー側から任意のタイミングで送信してみたり
			await Task.Delay(100);  // テキトーにずらしたり
			await streaming.WriteAsync(123);
			await streaming.WriteAsync(456);

			//--- メッセージの受信がすべて終わるまで待つ
			await task;

			//--- サーバーからの送信が完了したことを通知
			return streaming.Result();
		}


		// 独自型を受け取り
		public async UnaryResult<Nil> Sample(Vector2 point)
		{
			try {
				// 予期せぬエラーをthrowしてみる
				if (point.X == int.MinValue || point.X == int.MaxValue || point.Y == int.MinValue || point.Y == int.MaxValue)
					throw new ArgumentException("point");

				// ステータスコード ＋ エラー詳細を返すことも出来る
				if (point.X < 0 || point.Y < 0)
					throw new ArgumentOutOfRangeException($"X/Y must be 0 or more integer. (x,y)=({point.X}, {point.Y})");

				Logger.Debug($"Sample: (x, y) = ({point.X}, {point.Y})");
				return Nil.Default;  //--- 独自型を返せます

			} catch (ArgumentOutOfRangeException ex) {
				// 予期しているエラーをcatchした場合、ステータスコード + エラー詳細を返す
				return this.ReturnStatusCode<Nil>((int)StatusCode.Internal, ex.Message);
			}
		}

		// Metadataを受け取り
		public async UnaryResult<Nil> SampleWithMetadata()
		{
			//--- ヘッダーから値を取り出す
			var header = this.Context.CallContext.RequestHeaders;
			var value1 = header.Get("Key").Value;
			var value2 = header.Get("Key-bin").ValueBytes;

			Logger.Debug("[SampleWithMetadata]");
			Logger.Debug(value1);
			Logger.Debug($"{{{value2[0]}, {value2[1]}}}");

			return Nil.Default;
		}
	}
}
