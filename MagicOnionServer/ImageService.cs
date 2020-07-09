using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MagicOnion;
using MagicOnion.Server;
using MagicOnionExperiment.ServiceDefinitions;

namespace MagicOnionServer
{
	class ImageService : ServiceBase<IImageService>, IImageService
	{
		// min, max, ave を返す
		public async UnaryResult<(int, int, double)> SendImage(FrameImage image)
		{
			Logger.Debug($"[FrameImage] (w, h) = ({image.Width}, {image.Height})");

			int max = int.MinValue;
			int min = int.MaxValue;
			long sum = 0;
			Parallel.ForEach(image.Bytes, (b) => {
				sum += b;
				if (max < b) max = b;
				if (b < min) min = b;
			});

			return (min, max, (double)sum / (image.Width * image.Height));
		}
	}
}
