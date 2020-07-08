using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using MagicOnionExperiment.ServiceDefinitions;

namespace MagicOnionExperiment.Client
{
	class Program
	{
		static void Main() => MainAsync().Wait();

		static async Task MainAsync()
		{
			//--- API が公開されている IP / Port / 認証情報を設定して通信路を生成
			var channel = new Channel("localhost", 12345, ChannelCredentials.Insecure);

			//--- 指定されたサービス定義用のクライアントを作成
			var client = MagicOnionClient.Create<ISampleApi>(channel);
			var imageClient = MagicOnionClient.Create<IImageService>(channel);

			for (int i = 0; i < 1; i++) {
				// ■UnaryStreaming
				//--- RPC 形式で超お手軽呼び出し
				var result = await client.Sum(1, 2);
				Console.WriteLine($"[Unary] Result : {result}");

				// ■サーバーからの Streaming
				//--- サーバーが WriteAsync すると ForEachAsync が動く
				//--- サーバーから完了信号が送られると ForEachAsync が終了する
				var streaming = await client.Repeat(4, 5);
				await streaming.ResponseStream.ForEachAsync(x => {
					Console.WriteLine($"[Streaming] Result : {x}");
				});

				// ■Clientからの Streaming
				var clientStreaming = await client.SplitUpload();
				foreach (var x in Enumerable.Range(1, 4)) {
					await clientStreaming.RequestStream.WriteAsync(x);
				}
				// -- 完了通知
				await clientStreaming.RequestStream.CompleteAsync();
				// --- サーバーからの結果を取得
				var response = await clientStreaming.ResponseAsync;
				Console.WriteLine($"[ClientStreaming] Result : {response}");

				// ■双方向の Streaming
				// サーバー側からのメッセージを受信
				var duplexStreaming = await client.DuplexSample();
				var task = duplexStreaming.ResponseStream.ForEachAsync(x => {
					Console.WriteLine($"[DuplexStreaming] Received: {x}");
				});
				// クライアント側からサーバー側にメッセージを送信
				foreach (var x in Enumerable.Range(0, 5)) {
					await duplexStreaming.RequestStream.WriteAsync(x);
				}
				// メッセージ送信完了を通知
				await duplexStreaming.RequestStream.CompleteAsync();
				await task;

				// ■独自型の送信
				var point = new Vector2(3, 5);
				var resVec = await client.Sample(point);	// Nilが返ってくる
				Console.WriteLine($"独自型送信結果1: {resVec.GetType().FullName}");

				// ■ステータスコードの取得
				var call = client.Sample(new Vector2(1, 2));
				var header = await call.ResponseHeadersAsync;
				var status = call.GetStatus();  // ResponseHeadersAsyncしないとGetStatus()を呼び出せない

				if (status.StatusCode == StatusCode.OK) {
					// OKだったら結果を受け取る
					var resVec2 = await call.ResponseAsync;
					Console.WriteLine($"独自型送信結果2: 正常: {resVec}");
				} else {
					Console.WriteLine($"独自型送信結果2: エラー: {status}");
				}

				try {
					// OK 以外のステータスコードが送られているレスポンスに対して値を読み出そうとすると RpcException が発生する
					resVec = await client.Sample(new Vector2(-1, 2));
					Console.WriteLine($"独自型送信結果3: 正常: {resVec.GetType().FullName}");
				} catch (Grpc.Core.RpcException ex) {
					Console.WriteLine(ex.Message);
				}

				// サーバー側で予期せぬエラーが発生したら… ⇒ StatusCode.Unknown が返される
				call = client.Sample(new Vector2(int.MinValue, 2));
				header = await call.ResponseHeadersAsync;
				status = call.GetStatus();

				if (status.StatusCode == StatusCode.OK) {
					// OKだったら結果を受け取る
					var resVec2 = await call.ResponseAsync;
					Console.WriteLine($"独自型送信結果4: 正常: {resVec}");
				} else {
					// Status(StatusCode = Unknown, Detail = "Exception was thrown by handler.")
					Console.WriteLine($"独自型送信結果4: エラー: {status}");
				}
			}

			// BitmapImageの送信
			// Create source.
			BitmapImage bi = new BitmapImage();
			// BitmapImage.UriSource must be in a BeginInit/EndInit block.
			bi.BeginInit();
			bi.UriSource = new Uri(@"resources/image.axd.jpg", UriKind.RelativeOrAbsolute);
			bi.EndInit();
			bi.Freeze();

			int width = bi.PixelWidth;
			int height = bi.PixelHeight;
			int stride = (width * bi.Format.BitsPerPixel + 7) / 8;
			byte[] bytes = new byte[stride * height];
			bi.CopyPixels(bytes, stride, 0);

			var frame = new FrameImage(bi.PixelWidth, bi.PixelHeight, bytes);
			var (min, max, ave) = await imageClient.SendImage(frame);
			Console.WriteLine($"Frame送信結果: Min={min}, Max={max}, Ave={ave}");
			
			Console.ReadLine();
		}
	}
}
