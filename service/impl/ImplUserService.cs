using Dapper;
using Mymmorpg;

namespace GameServer.impl.service
{
    public interface IUserService
    {
        User GetUser(GetUserByIdRequest request);
        User LoginUser(LoginUserRequest request);
        User GetUserByParams(GetUserByParamsRequest request);
        bool AddUser(User user);
        bool UpdateUser(UpdateUserRequest request);
        bool DeleteUser(int userId);
    }
}