using MySql.Data.MySqlClient;
using Mymmorpg;
using GameServer.impl.service;
using Dapper;
using System.Data;

namespace GameServer.service
{
    public class CharacterService : ICharacterService
    {
        // 连接数据库
        private readonly string _connectionString = "server=localhost;user=root;password=peko2077;database=mymmorpg;Charset=utf8mb4;";

        // characterName, picPath, prefabPath, characterGender, characterBirthday, characterRemark, campId, characterClassId, characterSkillsId

        #region 添加角色
        public int AddCharacter(Character character)
        {
            try
            {
                using (IDbConnection db = new MySqlConnection(_connectionString))
                {
                    var columns = new List<string>();
                    var values = new List<string>();
                    var parameters = new DynamicParameters();

                    // 角色名称
                    columns.Add("characterName");
                    values.Add("@CharacterName");
                    parameters.Add("@CharacterName", character.CharacterName);

                    // 路径 头像图片 picPath
                    if (!string.IsNullOrEmpty(character.PicPath))
                    {
                        columns.Add("picPath");
                        values.Add("@PicPath");
                        parameters.Add("@PicPath", character.PicPath);
                    }

                    // 路径 预制体 prefabPath
                    if (!string.IsNullOrEmpty(character.PrefabPath))
                    {
                        columns.Add("prefabPath");
                        values.Add("@PrefabPath");
                        parameters.Add("@PrefabPath", character.PrefabPath);
                    }

                    // 角色性别 characterGender
                    if (character.CharacterGender != Gender.Unknown)
                    {
                        columns.Add("characterGender");
                        values.Add("@CharacterGender");
                        parameters.Add("@CharacterGender", (int)character.CharacterGender);
                    }

                    // 角色生日 characterBirthday
                    if (!string.IsNullOrEmpty(character.CharacterBirthday))
                    {
                        columns.Add("characterBirthday");
                        values.Add("@CharacterBirthday");
                        parameters.Add("@CharacterBirthday", character.CharacterBirthday);
                    }

                    // 角色介绍文本 characterRemark
                    if (!string.IsNullOrEmpty(character.CharacterRemark))
                    {
                        columns.Add("characterRemark");
                        values.Add("@CharacterRemark");
                        parameters.Add("@CharacterRemark", character.CharacterRemark);
                    }

                    // 阵营编号 campId
                    if (character.CampId >= 0)
                    {
                        columns.Add("campId");
                        values.Add("@CampId");
                        parameters.Add("@CampId", character.CampId);
                    }

                    // 角色职业编号 characterClassId
                    if (character.CharacterClassId >= 0)
                    {
                        columns.Add("characterClassId");
                        values.Add("@CharacterClassId");
                        parameters.Add("@CharacterClassId", character.CharacterClassId);
                    }

                    // 角色技能编号 characterSkillsId
                    if (character.CharacterSkillsId >= 0)
                    {
                        columns.Add("characterSkillsId");
                        values.Add("@CharacterSkillsId");
                        parameters.Add("@CharacterSkillsId", character.CharacterSkillsId);
                    }

                    // 如果没有字段可插入，返回 false
                    if (columns.Count == 0) return 0;

                    // 构建 SQL 插入语句
                    string sql = $"INSERT INTO characters ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)}); SELECT LAST_INSERT_ID()";

                    // 打印方便调试
                    Console.WriteLine($"[SQL]: {sql}");
                    foreach (var name in parameters.ParameterNames)
                    {
                        Console.WriteLine($"[Param] {name} = {parameters.Get<dynamic>(name)}");
                    }

                    // 执行插入操作(返回数据库自增的id)
                    int newId = db.ExecuteScalar<int>(sql, parameters);

                    return newId;  // 如果插入了行,返回该条记录的id
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CharacterService] [AddCharacter]: 错误: {ex}");
                return 0;
            }
        }
        #endregion


        // #region 获取角色 根据id
        // #endregion

        #region 获取角色 根据id
        public Character? GetCharacterById(GetCharacterByIdRequest request)
        {
            try
            {
                using (IDbConnection db = new MySqlConnection(_connectionString))
                {
                    int characterId = request.CharacterId;

                    DynamicParameters parameters = new();
                    parameters.Add("CharacterId", characterId);

                    string sql = "SELECT characterId, characterName, picPath, prefabPath, characterGender, characterBirthday, characterRemark, campId, characterClassId, characterSkillsId FROM characters WHERE characterId = @CharacterId";
                    Character? dto = db.QueryFirstOrDefault<Character>(sql, parameters);

                    if (dto == null) { return null; }

                    Character character = ToProto(dto);

                    return character;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CharacterService] [GetCharacterById]: 错误: {ex}");
                return null;
            }
        }
        #endregion

        #region 获取角色 多条件
        public List<Character>? GetCharacterByParams(GetCharacterByParamsRequest request)
        {
            try
            {
                using (IDbConnection db = new MySqlConnection(_connectionString))
                {
                    var whereClauses = new List<string>();
                    var parameters = new DynamicParameters();

                    CharacterFields fields = request.CharacterFields;

                    if (!string.IsNullOrEmpty(fields.CharacterName))
                    {
                        whereClauses.Add("characterName = @CharacterName");
                        parameters.Add("@CharacterName", fields.CharacterName);
                    }

                    if (!string.IsNullOrEmpty(fields.PicPath))
                    {
                        whereClauses.Add("picPath = @PicPath");
                        parameters.Add("@PicPath", fields.PicPath);
                    }

                    if (!string.IsNullOrEmpty(fields.PrefabPath))
                    {
                        whereClauses.Add("prefabPath = @PrefabPath");
                        parameters.Add("@PrefabPath", fields.PrefabPath);
                    }

                    if (fields.CharacterGender >= 0)
                    {
                        whereClauses.Add("characterGender = @CharacterGender");
                        parameters.Add("@CharacterGender", fields.CharacterGender);
                    }

                    if (!string.IsNullOrEmpty(fields.CharacterBirthday))
                    {
                        whereClauses.Add("characterBirthday = @CharacterBirthday");
                        parameters.Add("@CharacterBirthday", fields.CharacterBirthday);
                    }

                    if (!string.IsNullOrEmpty(fields.CharacterRemark))
                    {
                        whereClauses.Add("characterRemark = @CharacterRemark");
                        parameters.Add("@CharacterRemark", fields.CharacterRemark);
                    }

                    if (fields.CampId >= 0)
                    {
                        whereClauses.Add("campId = @CampId");
                        parameters.Add("@CampId", fields.CampId);
                    }

                    if (fields.CharacterClassId >= 0)
                    {
                        whereClauses.Add("characterClassId = @CharacterClassId");
                        parameters.Add("@CharacterClassId", fields.CharacterClassId);
                    }

                    if (fields.CharacterSkillsId >= 0)
                    {
                        whereClauses.Add("characterSkillsId = @CharacterSkillsId");
                        parameters.Add("@CharacterSkillsId", fields.CharacterSkillsId);
                    }

                    if (whereClauses.Count == 0)
                    {
                        Console.WriteLine("[CharacterService] [GetCharacterByParams]: 参数不足，无法构造查询。");
                        return null;
                    }

                    string whereSql = string.Join(" AND ", whereClauses);

                    string sql = $"SELECT * FROM characters WHERE {whereSql} LIMIT 1";

                    IEnumerable<Character> dtoList = db.Query<Character>(sql, parameters);

                    if (dtoList == null || !dtoList.Any()) return null;

                    return dtoList.Select(ToProto).ToList();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CharacterService] [GetCharacterByParams]: 错误: {ex}");
                return null;
            }
        }
        #endregion

        #region 修改角色
        public bool UpdateCharacter(UpdateCharacterRequest request)
        {
            try
            {
                using (IDbConnection db = new MySqlConnection(_connectionString))
                {
                    var sql = "UPDATE characters SET ";
                    List<string> updates = [];
                    DynamicParameters parameters = new();

                    CharacterFields fields = request.CharacterFields;

                    if (!string.IsNullOrEmpty(fields.CharacterName))
                    {
                        updates.Add("characterName = @CharacterName");
                        parameters.Add("CharacterName", fields.CharacterName);
                    }

                    if (!string.IsNullOrEmpty(fields.PicPath))
                    {
                        updates.Add("picPath = @PicPath");
                        parameters.Add("PicPath", fields.PicPath);
                    }

                    if (!string.IsNullOrEmpty(fields.PrefabPath))
                    {
                        updates.Add("prefabPath = @PrefabPath");
                        parameters.Add("PrefabPath", fields.PrefabPath);
                    }

                    if (fields.CharacterGender != Gender.Unknown)
                    {
                        updates.Add("characterGender = @CharacterGender");
                        parameters.Add("CharacterGender", (int)fields.CharacterGender);
                    }

                    if (!string.IsNullOrEmpty(fields.CharacterBirthday))
                    {
                        updates.Add("characterBirthday = @CharacterBirthday");
                        parameters.Add("CharacterBirthday", fields.CharacterBirthday);
                    }

                    if (!string.IsNullOrEmpty(fields.CharacterRemark))
                    {
                        updates.Add("characterRemark = @CharacterRemark");
                        parameters.Add("CharacterRemark", fields.CharacterRemark);
                    }

                    if (fields.CampId > 0)
                    {
                        updates.Add("campId = @CampId");
                        parameters.Add("CampId", fields.CampId);
                    }

                    if (fields.CharacterClassId > 0)
                    {
                        updates.Add("characterClassId = @CharacterClassId");
                        parameters.Add("CharacterClassId", fields.CharacterClassId);
                    }

                    if (fields.CharacterSkillsId > 0)
                    {
                        updates.Add("characterSkillsId = @CharacterSkillsId");
                        parameters.Add("CharacterSkillsId", fields.CharacterSkillsId);
                    }

                    // 如果没有需要更新的字段，返回 false
                    if (updates.Count == 0) return false;

                    // 完整的 SQL 更新语句
                    sql += string.Join(", ", updates) + " WHERE characterId = @CharacterId";
                    parameters.Add("CharacterId", request.CharacterId);

                    // 打印方便调试
                    Console.WriteLine($"[SQL]: {sql}");
                    foreach (var name in parameters.ParameterNames)
                    {
                        Console.WriteLine($"[Param] {name} = {parameters.Get<dynamic>(name)}");
                    }

                    int rowsAffected = db.Execute(sql, parameters);

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CharacterService] [UpdateCharacter]: 错误: {ex}");
                return false;
            }
        }
        #endregion

        #region DTO 转 Protobuf 对象
        private Character ToProto(Character dto)
        {
            return new Character
            {
                CharacterId = dto.CharacterId,
                CharacterName = dto.CharacterName ?? "",
                PicPath = dto.PicPath ?? "",
                PrefabPath = dto.PrefabPath ?? "",
                CharacterGender = dto.CharacterGender,
                CharacterBirthday = dto.CharacterBirthday ?? "",
                CampId = dto.CampId,
                CharacterClassId = dto.CharacterClassId,
                CharacterSkillsId = dto.CharacterSkillsId
            };
        }
        #endregion
    }
}