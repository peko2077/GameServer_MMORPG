using GameServer.impl.service;
using GameServer.service;
using GameServer.tools;
using Mymmorpg;

namespace GameServer.controller
{
    public class UserController
    {
        private readonly IUserService _userService;

        public UserController()
        {
            _userService = new UserService();  // 初始化 UserService
        }

        public void AddUser(ApiRequest request, ClientState state)
        {
            AddUserRequest addReq = request.AddUser;

            string userName = addReq.UserName;

            ApiResponse response = new ApiResponse();

            // 校验是否重名
            // 准备参数
            GetUserByParamsRequest paramReq = new GetUserByParamsRequest
            {
                UserName = userName
            };
            // 调用service层 GetUserByParams 多条件查询
            User resl = _userService.GetUserByParams(paramReq);
            if (resl != null)
            {
                response.Success = false;
                response.Message = $"{userName} 已存在, 请输入新的用户名";
                response.Error = "添加失败,该用户名已存在";
                ResponseHelper.SendResponse(response, state.Sender);
                return;
            }

            User user = new User
            {
                UserName = addReq.UserName ?? "",
                Password = addReq.Password ?? "",
                PhoneNumber = addReq.PhoneNumber ?? "",
                Email = addReq.Email ?? "",
                Age = addReq.Age
            };

            Console.WriteLine($"[AddUser] 用户名 = {user.UserName}, 密码 = {user.Password}");

            bool result = _userService.AddUser(user);

            if (result)
            {
                response.Success = true;
                response.Message = $"user 添加成功";
                response.Session.Data = new SessionTableData()
                {
                    User = user
                };
            }
            else
            {
                response.Success = false;
                response.Message = "user 添加失败";
                response.Error = "AddUser 执行添加操作失败";
            }

            ResponseHelper.SendResponse(response, state.Sender);
        }

        public void UpdateUser(ApiRequest request, ClientState state)
        {
            UpdateUserRequest updateReq = request.UpdateUser;

            if (updateReq.UserId <= 0)
            {
                ResponseHelper.SendResponse(new ApiResponse
                {
                    Message = "更新失败",
                    Success = false,
                    Error = "缺少或非法的 UserId"
                }, state.Sender);
                return;
            }

            // 校验：除了 playerId 外，至少有一个字段被设置才允许更新
            if (!updateReq.HasUserName &&
                !updateReq.HasPassword &&
                !updateReq.HasPhoneNumber &&
                !updateReq.HasEmail &&
                !updateReq.HasAge
                )
            {
                ResponseHelper.SendResponse(new ApiResponse
                {
                    Message = "更新失败",
                    Success = false,
                    Error = "没有提供任何要更新的字段"
                }, state.Sender);
                return;
            }

            bool result;
            try
            {
                result = _userService.UpdateUser(updateReq);
            }
            catch (Exception ex)
            {
                ResponseHelper.SendResponse(new ApiResponse
                {
                    Message = "更新失败",
                    Success = false,
                    Error = $"更新时发生异常: {ex.Message}"
                }, state.Sender);
                return;
            }

            if (result)
            {
                ResponseHelper.SendResponse(new ApiResponse
                {
                    Message = "user 更新成功",
                    Success = true
                }, state.Sender);
            }
            else
            {
                ResponseHelper.SendResponse(new ApiResponse
                {
                    Message = "user 更新失败",
                    Success = false,
                    Error = "数据库未更新任何记录, 可能playerId不存在"
                }, state.Sender);
            }
        }


        public void GetUserById(ApiRequest request, ClientState state)
        {
            GetUserByIdRequest userIdReq = request.GetUserById;
            int userId = userIdReq.UserId;
            if (userId <= 0)
            {
                ApiResponse errorResp = new ApiResponse
                {
                    Success = false,
                    Message = "用户编号不能为空",
                    Error = "[错误]GetUser, 请求中缺少 UserId 字段"
                };
                ResponseHelper.SendResponse(errorResp, state.Sender);
                return;
            }

            GetUserByIdRequest requestUserId = new GetUserByIdRequest
            {
                UserId = userId
            };

            User user = _userService.GetUser(requestUserId);

            ApiResponse response = new ApiResponse();
            if (user != null)
            {
                response.Success = true;
                response.Message = "user 获取成功！";
                response.Session.Data = new SessionTableData
                {
                    User = user
                };
            }
            else
            {
                response.Success = false;
                response.Message = "user 获取失败！";
                response.Error = "GetUser 失败, 数据库中无此用户";
            }

            ResponseHelper.SendResponse(response, state.Sender);
        }

        public void GetUserByParams(ApiRequest request, ClientState state)
        {

        }

        public void LoginUser(ApiRequest request, ClientState state)
        {
            LoginUserRequest loginReq = request.LoginUser;
            string userName = loginReq.UserName;
            string password = loginReq.Password;

            User user = _userService.LoginUser(new LoginUserRequest
            {
                UserName = userName,
                Password = password
            });

            Console.WriteLine($"[LoginUser] userName = {user.UserName}, password = {user.Password}");

            Console.WriteLine($"[LoginUser] command: {request.Command}");

            ApiResponse response = new ApiResponse()
            {
                Command = request.Command
            };

            if (user != null)
            {
                response.Success = true;
                response.Message = "登录成功";
                // 向客户端发送登录的具体数据
                response.Session = new Session()
                {
                    Data = new SessionTableData
                    {
                        User = user
                    }
                };
            }
            else
            {
                response.Success = false;
                response.Message = "登录失败";
                response.Error = "LoginUser 数据库查询失败";
            }

            // 发送响应
            ResponseHelper.SendResponse(response, state.Sender);
        }
    }
}