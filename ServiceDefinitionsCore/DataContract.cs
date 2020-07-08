using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace MagicOnionExperiment.ServiceDefinitions
{
	[MessagePackObject]
	public class FrameImage
	{
		[Key(0)]
		public int Width { get; }
		[Key(1)]
		public int Height { get; }
		[Key(2)]
		public byte[] Bytes { get; }

		public FrameImage(int width, int height, byte[] bytes)
		{
			Width = width;
			Height = height;
			Bytes = bytes;
		}
	}
}
