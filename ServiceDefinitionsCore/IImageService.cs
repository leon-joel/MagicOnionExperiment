using System;
using Grpc.Core;
using MagicOnion;
using MessagePack;

namespace MagicOnionExperiment.ServiceDefinitions
{
	public interface IImageService : IService<IImageService>
	{
		UnaryResult<(int, int, double)> SendImage(FrameImage image);
	}
}
