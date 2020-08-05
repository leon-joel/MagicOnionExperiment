using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MagicOnion;
using MessagePack;

namespace MagicOnionExperiment.ServiceDefinitions
{
    // サーバー -> クライアントの通信定義
    public interface IGamingHubReceiver
    {
        void OnJoin(Player player);
        void OnLeave(Player player);
    }

    // クライアント -> サーバーの通信定義
    public interface IGamingHub : IStreamingHub<IGamingHub, IGamingHubReceiver>
    {
        Task<Player[]> JoinAsync(string roomName, string userName);
        Task LeaveAsync();
    }

    // 送受信に使うカスタムオブジェクト
    [MessagePackObject]
    public class Player
    {
        [Key(0)]
        public string Name { get; set; }
    }
}
