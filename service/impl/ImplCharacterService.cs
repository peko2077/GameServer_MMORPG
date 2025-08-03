using Dapper;
using Mymmorpg;

namespace GameServer.impl.service
{
    public interface ICharacterService
    {
        // 添加角色
        int AddCharacter(Character character);
        // 修改角色
        bool UpdateCharacter(UpdateCharacterRequest request);
        // 获取角色(根据id)
        Character? GetCharacterById(GetCharacterByIdRequest request);
        // 获取角色(多条件)
        List<Character>? GetCharacterByParams(GetCharacterByParamsRequest request);
    }
}