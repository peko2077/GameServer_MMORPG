using MySql.Data.MySqlClient;
using Mymmorpg;
using GameServer.impl.service;
using System.Data;
using Dapper;
using GameServer.dto;

namespace GameServer.service
{
    public class UserService : IUserService
    {
        private readonly PlayerService _playerService = new PlayerService();

        // 连接数据库
        private readonly string _connectionString = "server=localhost;user=root;password=peko2077;database=mymmorpg;";

        public bool AddUser(User user)
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                var columns = new List<string>();
                var values = new List<string>();
                var parameters = new DynamicParameters();

                // 必填字段：用户名和密码
                columns.Add("userName");
                values.Add("@UserName");
                parameters.Add("@UserName", user.UserName);

                columns.Add("password");
                values.Add("@Password");
                parameters.Add("@Password", user.Password);

                // 可选字段：手机号
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    columns.Add("phoneNumber");
                    values.Add("@PhoneNumber");
                    parameters.Add("@PhoneNumber", user.PhoneNumber);
                }

                // 可选字段：邮箱
                if (!string.IsNullOrEmpty(user.Email))
                {
                    columns.Add("email");
                    values.Add("@Email");
                    parameters.Add("@Email", user.Email);
                }

                // 处理 nullable int（年龄字段）
                if (user.Age != 0)
                {
                    columns.Add("age");
                    values.Add("@Age");
                    parameters.Add("@Age", user.Age);  // 使用 Value 获取实际值
                }

                // 如果没有字段可插入，返回 false
                if (columns.Count == 0)
                    return false;

                // 构建 SQL 插入语句
                string sql = $"INSERT INTO users ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";

                // 执行插入操作并返回影响的行数
                int rowsAffected = db.Execute(sql, parameters);

                return rowsAffected > 0;  // 如果插入了行，返回 true
            }
        }

        public bool DeleteUser(int userId)
        {
            throw new NotImplementedException();
        }

        public User GetUser(GetUserByIdRequest request)
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                // 查询用户数据
                string sql = "SELECT userId, userName, password, phoneNumber, email, age FROM users WHERE userId = @UserId";
                var userDto = db.QueryFirstOrDefault<UserDto>(sql, new { UserId = request.UserId });
                if (userDto == null)
                    return null;

                // 转换为 Protobuf User 对象
                var user = ToProto(userDto);

                return user;
            }
        }

        public User GetUserByParams(GetUserByParamsRequest request)
        {
            // 构建基础 SQL 查询语句
            string sql = "SELECT userId, userName, password, phoneNumber, email, age FROM users WHERE 1 = 1";

            // 创建一个 Dictionary 来存储查询参数
            var param = new DynamicParameters();

            // HashSet 用来存储允许的字段名（这些字段会用来添加到 WHERE 子句中）
            HashSet<string> allowedFields = new HashSet<string> { "userName", "phoneNumber", "email", "age" };

            if (!string.IsNullOrEmpty(request.UserName))
            {
                sql += " AND userName = @UserName";  // 使用参数化查询，防止 SQL 注入
                param.Add("UserName", request.UserName);
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                sql += " AND phoneNumber = @PhoneNumber";  // 使用参数化查询，防止 SQL 注入
                param.Add("PhoneNumber", request.PhoneNumber);
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                sql += " AND email = @Email";  // 使用参数化查询，防止 SQL 注入
                param.Add("Email", request.Email);
            }

            if (request.Age > 0)
            {
                sql += " AND age = @Age";
                param.Add("Age", request.Age);
            }

            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                // 执行查询并获取匹配的用户
                var userDto = db.QueryFirstOrDefault<UserDto>(sql, param);

                // 如果没有查询到用户 返回空
                if (userDto == null) return null;

                // 打印获取到的数据库数据（user）
                Console.WriteLine($"userDto: UserId = {userDto.UserId}, UserName = {userDto.UserName}, Password = {userDto.Password}, PhoneNumber = {userDto.PhoneNumber}, Email = {userDto.Email}, Age = {userDto.Age}");

                // 转换为 Protobuf 的 User 对象
                User user = ToProto(userDto);

                return user;
            }
        }

        public User LoginUser(LoginUserRequest request)
        {
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                // 构建查询 SQL，根据用户名和密码验证
                string sql = "SELECT userId, userName, password, phoneNumber, email, age FROM users WHERE userName = @UserName AND password = @Password";

                // 执行查询并获取返回的用户信息
                var userDto = db.QueryFirstOrDefault<UserDto>(sql, new { UserName = request.UserName, Password = request.Password });

                // 如果没有找到用户或者密码不匹配，返回 null
                if (userDto == null)
                    return null;

                // 打印查询到的用户信息（可选，用于调试）
                Console.WriteLine($"登录成功: UserId = {userDto.UserId}, UserName = {userDto.UserName}");

                // 将查询到的用户数据转换为 Protobuf 的 User 类型
                User user = ToProto(userDto);

                return user;
            }
        }

        public bool UpdateUser(UpdateUserRequest request)
        {

            var sql = "UPDATE users SET ";
            var updates = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(request.UserName))
            {
                updates.Add("userName = @UserName");
                parameters.Add("UserName", request.UserName);
            }

            if (!string.IsNullOrEmpty(request.Password))
            {
                updates.Add("password = @Password");
                parameters.Add("Password", request.Password);
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                updates.Add("phoneNumber = @PhoneNumber");
                parameters.Add("PhoneNumber", request.PhoneNumber);
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                updates.Add("email = @Email");
                parameters.Add("Email", request.Email);
            }

            if (request.Age > 0)
            {
                updates.Add("age = @Age");
                parameters.Add("Age", request.Age);
            }

            if (updates.Count == 0) return false;

            // 构造完整 SQL
            sql += string.Join(", ", updates) + " WHERE userId = @UserId";
            parameters.Add("UserId", request.UserId);

            // 执行更新操作
            using (IDbConnection db = new MySqlConnection(_connectionString))
            {
                int rowsAffected = db.Execute(sql, parameters);
                // 如果更新的行数大于0，表示成功更新，返回 true
                return rowsAffected > 0;
            }
        }

        private User ToProto(UserDto dto)
        {
            return new User
            {
                UserId = dto.UserId,
                UserName = dto.UserName ?? "",
                Password = dto.Password ?? "",
                PhoneNumber = dto.PhoneNumber ?? "",
                Email = dto.Email ?? "",
                Age = dto.Age,
            };
        }
    }
}