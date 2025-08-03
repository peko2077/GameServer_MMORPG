using GameServer.tools;
using Mymmorpg;

namespace GameServer.controller
{
    public class ControllerManage
    {
        // 玩家相关操作处理器（如添加、更新、查询玩家）
        private readonly PlayerController _playerController = new();

        // 用户相关操作处理器（如注册、登录、更新用户信息）
        private readonly UserController _userController = new();

        /// <summary>
        /// 主分发方法：根据 ApiRequest 的 payload 类型，分发到对应 Controller。
        /// </summary>
        /// <param name="request">客户端发来的 API 请求（带 oneof payload）</param>
        /// <param name="state">客户端连接状态，包含 socket sender 等信息</param>
        public void HandleRequest(ApiRequest request, ClientState state)
        {
            switch (request.PayloadCase)
            {
                case ApiRequest.PayloadOneofCase.AddPlayer:
                    _playerController.AddPlayer(request, state);
                    break;

                case ApiRequest.PayloadOneofCase.GetPlayer:
                    _playerController.GetPlayer(request, state);
                    break;

                case ApiRequest.PayloadOneofCase.GetPlayersByUserId:
                    _playerController.GetPlayersByUserId(request, state);
                    break;

                case ApiRequest.PayloadOneofCase.UpdatePlayer:
                    _playerController.UpdatePlayer(request, state);
                    break;

                case ApiRequest.PayloadOneofCase.AddUser:
                    _userController.AddUser(request, state);
                    break;

                case ApiRequest.PayloadOneofCase.GetUserById:
                    _userController.GetUserById(request, state);
                    break;

                case ApiRequest.PayloadOneofCase.GetUserByParams:
                    _userController.GetUserByParams(request, state);
                    break;

                case ApiRequest.PayloadOneofCase.LoginUser:
                    _userController.LoginUser(request, state);
                    break;

                case ApiRequest.PayloadOneofCase.UpdateUser:
                    _userController.UpdateUser(request, state);
                    break;

                // 更多 case...

                // 如果 payload 不匹配任何定义的消息类型，返回错误响应
                default:
                    var response = new ApiResponse
                    {
                        Success = false,
                        Message = "未知请求类型",
                        Error = "PayloadCase: " + request.PayloadCase
                    };
                    ResponseHelper.SendResponse(response, state.Sender);
                    break;
            }

        }

    }

}