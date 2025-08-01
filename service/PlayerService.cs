using MySql.Data.MySqlClient;
using Mymmorpg;
using GameServer.impl.service;
using Dapper;
using System.Data;
using GameServer.dto;

namespace GameServer.service
{
    public class PlayerService : IPlayerService
    {
        public DateTime Birthday { get; set; }

        // 连接数据库
        private readonly string _connectionString = "server=localhost;user=root;password=peko2077;database=mymmorpg;Charset=utf8mb4;";

        public int AddPlayer(Player player)
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                var columns = new List<string>();
                var values = new List<string>();
                var parameters = new DynamicParameters();

                // 必填字段：玩家 名称
                columns.Add("playerName");
                values.Add("@PlayerName");
                parameters.Add("@PlayerName", player.PlayerName);

                // 必填字段：玩家 性别
                columns.Add("gender");
                values.Add("@Gender");
                parameters.Add("@Gender", player.Gender);

                // 可选字段：头像路径
                if (!string.IsNullOrEmpty(player.Pic))
                {
                    columns.Add("pic");
                    values.Add("@Pic");
                    parameters.Add("@Pic", player.Pic);
                }

                if (player.Level <= 0)
                {
                    columns.Add("level");
                    values.Add("@Level");
                    parameters.Add("@Level", player.Level);
                }

                if (player.Experience <= 0)
                {
                    columns.Add("experience");
                    values.Add("@Experience");
                    parameters.Add("@Experience", player.Experience);
                }

                // 如果生日存在，则插入生日字段
                if (!string.IsNullOrEmpty(player.Birthday))
                {
                    columns.Add("birthday");
                    values.Add("@Birthday");
                    parameters.Add("@Birthday", player.Birthday);
                }

                // 如果备注存在，则插入备注字段
                if (!string.IsNullOrEmpty(player.Remark))
                {
                    columns.Add("remark");
                    values.Add("@Remark");
                    parameters.Add("@Remark", player.Remark);
                }

                if (player.UserId < 0)
                {
                    columns.Add("userId");
                    values.Add("@UserId");
                    parameters.Add("@UserId", player.UserId);
                }

                // 如果没有字段可插入，返回 false
                if (columns.Count == 0) return 0;

                Console.WriteLine($"[PlayerService] [AddPlayer]: Player Name: {player.PlayerName}");

                // 构建 SQL 插入语句
                string sql = $"INSERT INTO players ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)}); SELECT LAST_INSERT_ID()";

                // 执行插入操作并返回影响的行数
                int newId = db.ExecuteScalar<int>(sql, parameters);

                return newId;  // 如果插入了行，返回 true
            }
        }

        public bool DeletePlayer(int playerId)
        {
            throw new NotImplementedException();
        }

        public Player GetPlayer(int playerId)
        {
            // 使用 MySQL 连接字符串创建数据库连接，并确保连接在使用完后自动关闭
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                // 定义 SQL 查询语句，使用参数化查询来避免 SQL 注入攻击
                string sql = "SELECT playerId, playerName, gender, pic, level, experience, birthday, remark, userId FROM players WHERE playerId = @PlayerId";

                // 使用 Dapper 执行 SQL 查询，返回第一个匹配的 PlayerDto 对象
                // 如果查询没有结果，返回 null
                var dto = db.QueryFirstOrDefault<PlayerDto>(sql, new { PlayerId = playerId });

                // 如果查询结果为空，表示没有找到对应的玩家，直接返回 null
                if (dto == null)
                    return null;

                // 查询到数据后，调用 ToProto 方法将 PlayerDto 转换为 Protobuf 的 Player 对象
                // Protobuf 格式用于高效传输数据，适用于跨平台数据交换
                var player = ToProto(dto);

                // 返回player
                return player;
            }
        }

        public bool UpdatePlayer(UpdatePlayerRequest request)
        {
            var sql = "UPDATE players SET ";
            var updates = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(request.PlayerName))
            {
                updates.Add("playerName = @PlayerName");
                parameters.Add("PlayerName", request.PlayerName);
            }

            if (request.HasGender)
            {
                updates.Add("gender = @Gender");
                parameters.Add("Gender", request.Gender);
            }

            if (!string.IsNullOrEmpty(request.Pic))
            {
                updates.Add("pic = @Pic");
                parameters.Add("Pic", request.Pic);
            }

            if (request.Level != 0)
            {
                updates.Add("level = @Level");
                parameters.Add("Level", request.Level);
            }

            if (request.Experience != 0)
            {
                updates.Add("experience = @Experience");
                parameters.Add("Experience", request.Experience);
            }

            if (!string.IsNullOrEmpty(request.Birthday))
            {
                updates.Add("birthday = @Birthday");
                parameters.Add("Birthday", request.Birthday);
            }

            if (!string.IsNullOrEmpty(request.Remark))
            {
                updates.Add("remark = @Remark");
                parameters.Add("Remark", request.Remark);
            }

            if (request.UserId != 0)
            {
                updates.Add("userId = @UserId");
                parameters.Add("UserId", request.UserId);
            }

            // 如果没有需要更新的字段，返回 false
            if (updates.Count == 0) return false;

            // 完整的 SQL 更新语句
            sql += string.Join(", ", updates) + " WHERE playerId = @PlayerId";
            parameters.Add("PlayerId", request.PlayerId);

            // 执行更新操作
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                int rowsAffected = db.Execute(sql, parameters);
                // 如果更新的行数大于0，表示成功更新，返回 true
                return rowsAffected > 0;
            }
        }

        // DTO 转 Protobuf 对象
        private Player ToProto(PlayerDto dto)
        {
            return new Player
            {
                PlayerId = dto.PlayerId,
                PlayerName = dto.PlayerName ?? "",
                Gender = dto.Gender,
                Pic = dto.Pic ?? "",
                Level = dto.Level,
                Experience = dto.Experience,
                Birthday = dto.Birthday?.ToString("yyyy-MM-dd") ?? "",
                Remark = dto.Remark ?? "",
                UserId = dto.UserId
            };
        }
    }
}