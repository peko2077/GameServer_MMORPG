using Dapper;
using Mymmorpg;

namespace GameServer.impl.service
{
    public interface IPlayerService
    {
        Player GetPlayer(int playerId);
        int AddPlayer(Player player);
        bool UpdatePlayer(UpdatePlayerRequest request);
        bool DeletePlayer(int playerId);
    }
}