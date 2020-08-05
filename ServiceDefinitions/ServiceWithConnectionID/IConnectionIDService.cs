using System;
using System.Threading.Tasks;
using MagicOnion;
using MessagePack;

namespace MagicOnionExperiment.ServiceDefinitions
{
	public interface IConnectionIDService : IService<IConnectionIDService>
	{
		public UnaryResult<Nil> SendConnectionID();
    }
}
