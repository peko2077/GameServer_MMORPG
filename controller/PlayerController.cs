using GameServer.impl.service;
using GameServer.service;
using GameServer.tools;
using Mymmorpg;
using System.Net.Sockets;
namespace GameServer.controller
{
    // 玩家控制器类，用于处理客户端发来的 player 请求
    public class PlayerController
    {
        // 玩家服务接口，用于业务逻辑处理（如数据库操作）
        private readonly IPlayerService _playerService;

        // 构造函数，初始化具体实现类
        public PlayerController()
        {
            _playerService = new PlayerService();// 绑定实际的业务实现
        }

        #region 添加player
        public void AddPlayer(ApiRequest request, ClientState state)
        {

            if (request.PayloadCase != ApiRequest.PayloadOneofCase.AddPlayer || request.AddPlayer == null)
            {
                Console.WriteLine("[AddPlayer] AddPlayer 字段为空！");
                ApiResponse resp = new ApiResponse
                {
                    Success = false,
                    Message = "AddPlayer 请求体为空，可能客户端未正确传入 payload",
                    Error = "AddPlayer is null"
                };
                ResponseHelper.SendResponse(resp, state.Sender);
                return;
            }

            Console.WriteLine($"[AddPlayer] PayloadCase = {request.PayloadCase}");

            AddPlayerRequest addReq = request.AddPlayer;

            // 校验 玩家名字是否为空
            if (string.IsNullOrWhiteSpace(addReq.PlayerName))
            {
                ApiResponse responseError = new ApiResponse
                {
                    Success = false,
                    Message = "玩家姓名不能为空",
                    Error = "PlayerName为空"
                };
                // 发送响应给客户端
                ResponseHelper.SendResponse(responseError, state.Sender);
                return;
            }

            string defaultPic = "resource/img/pic/default.png";

            // 构造 Player 对象，用于传递给 Service 层
            Player player = new Player
            {
                PlayerName = request.AddPlayer.PlayerName,
                Gender = request.AddPlayer.Gender,
                Pic = defaultPic,
                Level = 0,
                Experience = 0,
                Birthday = request.AddPlayer.Birthday ?? "",
                Remark = request.AddPlayer.Remark ?? "",
                UserId = request.AddPlayer.UserId
            };

            // 打印日志，便于调试
            Console.WriteLine($"[PlayerController] AddPlayer: playerName = {player.PlayerName}, gender = {player.Gender}, pic = {player.Pic}, level = {player.Level}, experience = {player.Experience}, birthday = {player.Birthday}, experience = {player.Experience}, userId = {player.UserId}");

            // 调用 Service 中的方法插入数据库，并返回新插入的主键ID
            int newPlayerId = _playerService.AddPlayer(player);

            // 构建返回结果
            ApiResponse response = new ApiResponse();
            if (newPlayerId > 0)
            {
                // 插入成功，设置 PlayerId 并返回成功响应
                player.PlayerId = newPlayerId;
                response.Success = true;
                response.Message = $"player 添加成功, playerId = {newPlayerId}";
            }
            else
            {
                // 插入失败，返回错误信息
                response.Success = false;
                response.Message = "player 添加失败";
                response.Error = "AddPlayer执行添加操作, 数据库操作失败";
            }

            // 发送响应给客户端
            ResponseHelper.SendResponse(response, state.Sender);
        }
        #endregion

        #region 根据 playerId 查询player
        public void GetPlayer(ApiRequest request, ClientState state)
        {

            int playerId = request.GetPlayer.PlayerId;
            if (!request.GetPlayer.HasPlayerId)
            {
                ApiResponse errorResp = new ApiResponse
                {
                    Success = false,
                    Message = "玩家编号不能为空",
                    Error = "[错误]GetPlayer, 请求中缺少 playerId 字段"
                };
                ResponseHelper.SendResponse(errorResp, state.Sender);
                return;
            }

            // 将必要查询参数传给GetPlayer方法，并接收查询结果
            Player player = _playerService.GetPlayer(playerId);
            ApiResponse response = new ApiResponse();

            // 构建返回结果
            if (player != null)
            {
                response.Success = true;
                response.Message = "玩家 获取成功";
                response.Session.Data = new SessionTableData()
                {
                    Player = player
                };
            }
            else
            {
                response.Success = false;
                response.Message = "玩家 获取失败";
                response.Error = "GetPlayer获取失败, 数据库查询失败";
            }

            // 发送响应给客户端
            ResponseHelper.SendResponse(response, state.Sender);
        }
        #endregion

        #region 根据 userId 查询player
        public void GetPlayersByUserId(ApiRequest request, ClientState state)
        {
            int userId = request.GetPlayersByUserId.UserId;

            // 校验 userId 合法性
            if (userId <= 0)
            {
                ApiResponse errorResp = new ApiResponse
                {
                    Command = request.Command,
                    Success = false,
                    Message = "[PlayerController] userId 不能为空",
                    Error = "[错误] GetPlayersByUserId, userId 无效"
                };
                ResponseHelper.SendResponse(errorResp, state.Sender);
                return;
            }

            GetPlayersByUserIdRequest req = new()
            {
                UserId = userId,
            };

            List<Player>? players = _playerService.GetPlayersByUserId(req);

            ApiResponse response = new ApiResponse()
            {
                Command = request.Command,
                Session = new Session()
            };

            if (players != null && players.Count > 0)
            {
                response.Success = true;
                response.Message = "玩家 获取成功";

                // 构建List返回集合
                SessionTableData data = new();
                data.Players.AddRange(players);

                // 将List 放进session的data中 响应给客户端
                response.Session.Data = data;
            }
            else
            {
                response.Success = false;
                response.Message = "玩家 获取失败";
                response.Error = "[GetPlayersByUserId] 数据库查询失败";
            }

            // 发送响应给客户端
            ResponseHelper.SendResponse(response, state.Sender);
        }
        #endregion

        #region 修改player
        public void UpdatePlayer(ApiRequest request, ClientState state)
        {
            UpdatePlayerRequest req = request.UpdatePlayer;
            // 必须传入 playerId
            if (req.PlayerId <= 0)
            {
                ResponseHelper.SendResponse(new ApiResponse
                {
                    Message = "更新失败",
                    Success = false,
                    Error = "缺少有效的 playerId"
                }, state.Sender);
                return;
            }

            // 校验：除了 playerId 外，至少有一个字段被设置才允许更新
            if (!req.HasPlayerName &&
                !req.HasGender &&
                !req.HasPic &&
                !req.HasLevel &&
                !req.HasExperience &&
                !req.HasBirthday &&
                !req.HasRemark &&
                !req.HasUserId)
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
                result = _playerService.UpdatePlayer(req);
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
                    Message = "player 更新成功",
                    Success = true
                }, state.Sender);
            }
            else
            {
                ResponseHelper.SendResponse(new ApiResponse
                {
                    Message = "player 更新失败",
                    Success = false,
                    Error = "数据库未更新任何记录, 可能playerId不存在"
                }, state.Sender);
            }

        }
        #endregion
    }

}