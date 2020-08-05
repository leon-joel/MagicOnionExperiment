using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace MagicOnionExperiment.ServiceDefinitions
{
	[MessagePackObject]
	public class Vector2
	{
		//--- バイナリのレイアウトの順番を設定
		[Key(0)]
		public float X { get; }

		[Key(1)]
		public float Y { get; }

		//--- デシリアライズに使うコンストラクタであることをマーク
		//--- ※ルールに沿っていれば必須ではない
		[SerializationConstructor]
		public Vector2(float x, float y)
		{
			this.X = x;
			this.Y = y;
		}
	}
}
