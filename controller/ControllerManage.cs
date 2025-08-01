using System.Net.Sockets;
using GameServer.tools;
using Mymmorpg;

namespace GameServer.controller
{
    public class ControllerManage
    {
        private readonly PlayerController _playerController = new PlayerController();
        private readonly UserController _userController = new UserController();

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