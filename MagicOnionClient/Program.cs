using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using MagicOnionExperiment.ServiceDefinitions;

namespace MagicOnionExperiment.Client
{
	class Program
	{
		public static int IConnectionIDService { get; private set; }

		static void Main() => MainAsync().Wait();

		static async Task MainAsync()
		{
			//--- API が公開されている IP / Port / 認証情報を設定して通信路を生成
			var channel = new Channel("localhost", 12345, ChannelCredentials.Insecure, new[]{
				new ChannelOption(ChannelOptions.MaxReceiveMessageLength, int.MaxValue),
				new ChannelOption(ChannelOptions.MaxSendMessageLength, int.MaxValue),
			});

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
				var resVec = await client.Sample(point);    // Nilが返ってくる
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

				// ■ヘッダーにデータを詰め込む
				// ※string, byte[] を格納できる
				// ※byte[] を格納する場合はキー名に "-bin" を付けること
				var metadata = new Metadata() {
					new Metadata.Entry("Key", "Value" ),
					new Metadata.Entry("Key-bin", new byte[]{1, 2}),
				};
				//var clientWithMetadata = MagicOnionClient.Create<ISampleApi>(channel).WithHeaders(metadata);
				//var resWithMetadata = await clientWithMetadata.SampleWithMetadata();
				// ↑でもいいけど、新しくclientを作らなくても大丈夫↓
				var resWithMetadata = await client.WithHeaders(metadata).SampleWithMetadata();
				Console.WriteLine($"メタデータ結果: 正常: {resWithMetadata.GetType().FullName}");
			}

			// byte[]の送信
			BitmapImage bi = LoadImageFile(@"resources/image.axd.jpg");
			byte[] bytes = ExtractByteArray(bi);
			var frame = new FrameImage(bi.PixelWidth, bi.PixelHeight, bytes);
			var (min, max, ave) = await imageClient.SendImage(frame);
			Console.WriteLine($"Frame送信結果: bytes={bytes.Length},  Min={min}, Max={max}, Ave={ave}");

			// 巨大なbyte[]の送信
			try {
				bi = LoadImageFile(@"resources/jem_40mb.tif");
				bytes = ExtractByteArray(bi);
				frame = new FrameImage(bi.PixelWidth, bi.PixelHeight, bytes);
				(min, max, ave) = await imageClient.SendImage(frame);
				Console.WriteLine($"Frame送信結果: bytes={bytes.Length},  Min={min}, Max={max}, Ave={ave}");
			}catch (RpcException ex) {
				// デフォルトでは4MBを越えるとエラーになる
				Console.WriteLine(ex.Message);
			}

			// ■ユーザー固有IDの送信
			// ★2020/08/05現在(MagicOnion3.0.13では) ChannelContext というクラスが存在しない！廃止された？！
			//--- ChannelContext でチャンネルとユーザー固有の ID をラップ
			//var connectionId = "なにかしらユーザー固有のID";
			//var context = new ChannelContext(channel, () => connectionId);
			//await context.WaitConnectComplete();  // 接続待ち
			//var client = context.CreateClient<ISampleApi>();
			//var result = await client.Sample();
			// ★仕方ないので、自前でヘッダーに仕込んで送るようにする
			var meta = new Metadata() {
				new Metadata.Entry("ConnectionId_ja", "なにかのID文字列"),
				new Metadata.Entry("ConnectionId", "some id string"),
			};
			var connectionIDClient = MagicOnionClient.Create<IConnectionIDService>(channel).WithHeaders(meta);
			var res = await connectionIDClient.SendConnectionID();

			// ■ゲームサーバーへの接続 （部屋への登録/解除、サーバーからのpush通知受け取り）
			GamingHubClient gamingHub = new GamingHubClient();
			string name = $"p{System.Diagnostics.Process.GetCurrentProcess().Id}";
			var gameObject = await gamingHub.ConnectAsync(channel, "room1", name);
			Console.ReadLine();

			await gamingHub.LeaveAsync();
			Console.ReadLine();
		}

		private static byte[] ExtractByteArray(BitmapImage bi)
		{
			int width = bi.PixelWidth;
			int height = bi.PixelHeight;
			int stride = (width * bi.Format.BitsPerPixel + 7) / 8;
			byte[] bytes = new byte[stride * height];
			bi.CopyPixels(bytes, stride, 0);
			return bytes;
		}

		private static BitmapImage LoadImageFile(string filePath)
		{
			BitmapImage bi = new BitmapImage();
			// BitmapImage.UriSource must be in a BeginInit/EndInit block.
			bi.BeginInit();
			bi.UriSource = new Uri(filePath, UriKind.RelativeOrAbsolute);
			bi.EndInit();
			bi.Freeze();
			return bi;
		}
	}
}
