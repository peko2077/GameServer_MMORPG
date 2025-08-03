using GameServer.impl.service;
using GameServer.service;
using GameServer.tools;
using Mymmorpg;
namespace GameServer.controller
{
    public class CharacterController
    {
        private readonly ICharacterService _characterService;

        public CharacterController()
        {
            _characterService = new CharacterService();
        }

        #region ======= Character 添加 =======
        public void AddCharacter(ApiRequest request, ClientState state)
        {
            CharacterFields fields = request.AddCharacter.CharacterFields;

            // 校验 添加的角色名称是否为空
            if (string.IsNullOrWhiteSpace(fields.CharacterName))
            {
                ApiResponse nameError = new()
                {
                    Success = false,
                    Message = "角色名称 不能为空",
                    Error = "characterName 为空"
                };
                // 发送响应给客户端
                ResponseHelper.SendResponse(nameError, state.Sender);
                return;
            }

            string picPath;
            string defaultPicPath = "resource/img/pic/default.png";
            if (string.IsNullOrEmpty(fields.PicPath))
            {
                picPath = defaultPicPath;
            }
            else
            {
                picPath = fields.PicPath;
            }

            string prefabPath;
            string defaultPrefabPath = "resource/img/pic/default.png";
            if (string.IsNullOrEmpty(fields.PrefabPath))
            {
                prefabPath = defaultPrefabPath;
            }
            else
            {
                prefabPath = fields.PrefabPath;
            }

            Character character = new()
            {
                CharacterName = fields.CharacterName,
                PicPath = picPath,
                PrefabPath = prefabPath,
                CharacterGender = fields.CharacterGender,
                CharacterBirthday = fields.CharacterBirthday,
                CharacterRemark = fields.CharacterRemark,
                CampId = fields.CampId,
                CharacterClassId = fields.CharacterClassId,
                CharacterSkillsId = fields.CharacterSkillsId,
            };

            // 打印日志，便于调试
            // Console.WriteLine($"[CharacterController] [AddCharacter] character: CharacterName = {character.CharacterName}, CharacterGender = {character.CharacterGender}, PicPath = {character.PicPath}, CharacterBirthday = {character.CharacterBirthday}, CharacterRemark = {character.CharacterRemark}, CampId = {character.CampId}, CharacterClassId = {character.CharacterClassId}, CharacterSkillsId = {character.CharacterSkillsId}");
            Console.WriteLine("[CharacterController] [AddCharacter] character:\n" +
                                $"  CharacterName     = {character.CharacterName}\n" +
                                $"  CharacterGender   = {character.CharacterGender}\n" +
                                $"  PicPath           = {character.PicPath}\n" +
                                $"  PrefabPath        = {character.PrefabPath}\n" +
                                $"  CharacterBirthday = {character.CharacterBirthday}\n" +
                                $"  CharacterRemark   = {character.CharacterRemark}\n" +
                                $"  CampId            = {character.CampId}\n" +
                                $"  CharacterClassId  = {character.CharacterClassId}\n" +
                                $"  CharacterSkillsId = {character.CharacterSkillsId}");

            // 调用服务 添加角色方法
            int newCharacterId = _characterService.AddCharacter(character);

            // 构建返回结果
            ApiResponse response = new ApiResponse();
            if (newCharacterId > 0)
            {
                response.Success = true;
                response.Message = $"character 添加成功, id = {newCharacterId}";
            }
            else
            {
                // 插入失败，返回错误信息
                response.Success = false;
                response.Message = "character 添加失败";
                response.Error = "AddCharacter 执行添加操作, 数据库操作失败";
            }

            // 发送响应给客户端
            ResponseHelper.SendResponse(response, state.Sender);
        }
        #endregion

        #region ======= Character 修改 =======
        public void UpdateCharacter(ApiRequest request, ClientState state)
        {
            UpdateCharacterRequest req = request.UpdateCharacter;

            int characterId = req.CharacterId;
            if (characterId <= 0)
            {
                Console.WriteLine("[CharacterController] [UpdatePlayer]: characterId 不合法...");

                ApiResponse invalidIdResponse = new()
                {
                    Success = false,
                    Message = "characterId 非法, 请重新提交有效的角色id",
                    Error = "参数 characterId 无效"
                };

                ResponseHelper.SendResponse(invalidIdResponse, state.Sender);
                return;
            }

            CharacterFields fields = req.CharacterFields;
            // 如果fields为空,就直接返回
            if (fields == null)
            {
                Console.WriteLine("[CharacterController] [UpdatePlayer]: CharacterFields 不能为空...");

                ApiResponse emptyFieldsResponse = new()
                {
                    Success = false,
                    Message = "CharacterFields 不能为空, 请至少提交要修改的字段",
                    Error = "UpdateCharacterRequest.CharacterFields 为 null"
                };
                ResponseHelper.SendResponse(emptyFieldsResponse, state.Sender);

                return;
            }

            bool result = _characterService.UpdateCharacter(req);
            ApiResponse response = new();
            if (result)
            {
                response.Success = result;
                response.Message = "character 修改成功";
            }
            else
            {
                response.Success = result;
                response.Message = "character 修改失败";
            }

            // 返回结果
            ResponseHelper.SendResponse(response, state.Sender);
        }
        #endregion

        #region ======= Character 根据 characterId 获取 =======
        public void GetCharacterById(ApiRequest request, ClientState state)
        {
            int characterId = request.GetCharacterById.CharacterId;
            if (characterId <= 0)
            {
                ApiResponse errorResp = new ApiResponse
                {
                    Success = false,
                    Message = "characterId 不存在, 请提交正确的角色id...",
                    Error = $"characterId = {characterId}, 请检查该角色是否存在..."
                };
                ResponseHelper.SendResponse(errorResp, state.Sender);
                return;
            }

            GetCharacterByIdRequest characterIdReq = new()
            {
                CharacterId = characterId
            };

            Character? character = _characterService.GetCharacterById(characterIdReq);

            ApiResponse response = new ApiResponse();

            // 构建返回结果
            if (character != null)
            {
                response.Success = true;
                response.Message = "character 获取成功";
                response.Session = new Session
                {
                    SessionId = "abcd1234",

                };
            }
            else
            {
                response.Success = false;
                response.Message = "character 获取失败";
                response.Error = "[CharacterController] [GetCharacterById]: character 获取失败, 数据库查询失败";
            }

            // 发送响应给客户端
            ResponseHelper.SendResponse(response, state.Sender);
        }
        #endregion

        #region ======= Character 多条件 获取 =======
        public void GetCharacterByParams(ApiRequest request, ClientState state)
        {
            CharacterFields fields = request.GetCharacterByParams.CharacterFields;
            if (fields == null)
            {
                ApiResponse errorResp = new ApiResponse
                {
                    Success = false,
                    Message = "fields 为空, 请提交有效",
                    Error = $"fields == null, 请提交有效字段..."
                };
                ResponseHelper.SendResponse(errorResp, state.Sender);
                return;
            }

            GetCharacterByParamsRequest paramsReq = new()
            {
                CharacterFields = fields
            };

            List<Character>? characters = _characterService.GetCharacterByParams(paramsReq);

            ApiResponse response = new();
            if (characters != null)
            {
                response.Success = true;
                response.Message = "character 获取成功";
                response.Data = new ResponseData();
                response.Data.CharacterList.AddRange(characters);
            }
            else
            {
                response.Success = false;
                response.Message = "character 获取失败";
                response.Error = "[CharacterController] [GetCharacterByParams]: character 获取失败, 数据库查询失败";
            }
            ResponseHelper.SendResponse(response, state.Sender);
        }
        #endregion

    }
}