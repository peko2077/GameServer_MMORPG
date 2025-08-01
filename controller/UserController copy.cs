// using Dapper;
// using GameServer.impl.service;
// using GameServer.service;

// using Google.Protobuf;
// using Mymmorpg;

// using System.Net;
// using System.Net.Sockets;
// using System.Text;

// namespace GameServer.controller
// {
//     public class UserController
//     {
//         private readonly IUserService _userService;

//         public UserController()
//         {
//             _userService = new UserService();  // 初始化 UserService
//         }

//         // 处理客户端请求
//         public void HandleClient(TcpClient client)
//         {
//             NetworkStream stream = client.GetStream();
//             try
//             {
//                 // 接收客户端请求
//                 byte[] buffer = new byte[1024];
//                 int bytesRead = stream.Read(buffer, 0, buffer.Length);
//                 string requestMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
//                 Console.WriteLine($"接收到客户端请求: {requestMessage}");

//                 // 解析客户端请求
//                 string[] requestParts = requestMessage.Split(':');
//                 Console.WriteLine("[UserController] 请求解析: " + string.Join(", ", requestParts));

//                 if (requestParts[0] == "GetUser" && int.TryParse(requestParts[1], out int userId))
//                 {
//                     // 获取用户
//                     User user = _userService.GetUser(userId);
//                     ApiResponse response;
//                     if (user != null)
//                     {
//                         response = new ApiResponse
//                         {
//                             Message = "获取用户成功",
//                             Success = true,
//                             User = user
//                         };
//                     }
//                     else
//                     {
//                         response = new ApiResponse
//                         {
//                             Message = "未找到用户",
//                             Success = false,
//                             Error = "用户不存在"
//                         };
//                     }
//                     SendResponse(response, stream);
//                 }
//                 else if (requestParts[0] == "AddUser")
//                 {
//                     // 检查请求参数长度，确保至少有用户名和密码两个参数
//                     if (requestParts.Length >= 3)
//                     {
//                         // 从请求参数中读取用户名
//                         string userName = requestParts[1];
//                         // 从请求参数中读取密码
//                         string password = requestParts[2];

//                         // 读取手机号参数，如果不存在则默认""
//                         string phoneNumber = requestParts.Length > 3 ? requestParts[3] : "";

//                         // 读取邮箱参数，如果不存在则则默认""
//                         string email = requestParts.Length > 4 ? requestParts[4] : "";

//                         // 读取年龄参数，默认值为0
//                         int age = 0;  // 默认值
//                         if (requestParts.Length > 5)
//                         {
//                             int.TryParse(requestParts[5], out age);
//                         }

//                         // 读取玩家ID参数，默认值为0
//                         int playerId = 0;  // 默认值
//                         if (requestParts.Length > 6)
//                         {
//                             int.TryParse(requestParts[6], out playerId);
//                         }

//                         // 创建动态参数来查询是否有重复用户名
//                         var queryParams = new List<QueryParam>
//                         {
//                             new QueryParam { Key = "userName", Value = userName }
//                         };

//                         var dynamicParams = new DynamicParameters();
//                         foreach (var param in queryParams)
//                         {
//                             dynamicParams.Add(param.Key, param.Value);
//                         }

//                         // 调用 GetUserByParams 查询是否有同名的用户
//                         User existingUser = _userService.GetUserByParams(dynamicParams);

//                         ApiResponse response = new ApiResponse();

//                         if (existingUser != null)
//                         {
//                             // 如果查询到用户，说明用户名已存在，返回错误信息
//                             response.Success = false;
//                             response.Message = "添加用户失败：用户名已存在！";
//                             response.Error = "用户名已存在！";
//                             SendResponse(response, stream);  // 发送失败响应给客户端
//                             return;  // 直接返回，不继续执行下面的添加操作
//                         }

//                         // 根据读取到的参数，创建一个新的 User 对象
//                         var user = new User
//                         {
//                             UserName = userName,
//                             Password = password,
//                             PhoneNumber = phoneNumber,   // 如果手机号没有传递，默认为 null
//                             Email = email,               // 如果邮箱没有传递，默认为 null
//                             Age = age,                   // 如果没有传递，默认为 0
//                             PlayerId = playerId          // 如果没有传递，默认为 0
//                         };

//                         // 打印要传递的 User 对象的数据
//                         Console.WriteLine($"User Data: UserName={user.UserName}, Password={user.Password}, PhoneNumber={user.PhoneNumber}, Email={user.Email}, Age={user.Age}, PlayerId={user.PlayerId}");

//                         // 调用服务层方法，尝试将新用户添加到数据库
//                         bool result = _userService.AddUser(user);

//                         if (result)
//                         {

//                             // 查询用户并填充响应
//                             User addedUser = _userService.GetUserByParams(dynamicParams);

//                             if (addedUser != null)
//                             {
//                                 response.Success = true;
//                                 response.Message = "用户添加成功";
//                                 response.User = addedUser;  // 将查询到的用户信息返回
//                             }
//                             else
//                             {
//                                 response.Success = false;
//                                 response.Message = "用户添加成功，但查询用户失败";
//                                 response.Error = "未找到用户信息";
//                             }

//                             // 将 ApiResponse 发送给客户端
//                             SendResponse(response, stream);

//                         }
//                         else
//                         {
//                             response.Success = false;
//                             response.Message = "用户添加失败";
//                             response.Error = "数据库插入失败或参数错误";
//                         }
//                     }
//                     else
//                     {
//                         // 请求参数不足，构造失败响应，告知客户端缺少用户名或密码
//                         var response = new ApiResponse
//                         {
//                             Success = false,
//                             Message = "添加用户失败：缺少用户名或密码",
//                             Error = "参数不足"
//                         };

//                         // 发送失败响应给客户端
//                         SendResponse(response, stream);
//                     }
//                 }
//                 else if (requestParts[0] == "DeleteUser" && int.TryParse(requestParts[1], out int userIdToDelete))
//                 {

//                 }
//                 else if (requestParts[0] == "UpdateUser")
//                 {

//                 }
//                 else if (requestParts[0] == "LoginUser")
//                 {
//                     if (requestParts.Length >= 3)
//                     {
//                         string userName = requestParts[1];
//                         string password = requestParts[2];

//                         User loginUser = _userService.LoginUser(userName, password);
//                         ApiResponse response;

//                         if (loginUser != null)
//                         {
//                             response = new ApiResponse
//                             {
//                                 Message = "登录成功！",
//                                 Success = true,
//                                 User = loginUser
//                             };

//                         }
//                         else
//                         {
//                             response = new ApiResponse
//                             {
//                                 Message = "登录失败：用户名或密码错误",
//                                 Success = false,
//                                 Error = "用户名或密码错误"
//                             };
//                         }
//                         SendResponse(response, stream);
//                     }
//                     else
//                     {
//                         var response = new ApiResponse
//                         {
//                             Message = "请求格式错误：缺少用户名或密码",
//                             Success = false,
//                             Error = "请求格式错误"
//                         };
//                         SendResponse(response, stream);
//                     }
//                 }
//                 else if (requestParts[0] == "GetUserByParams")
//                 {
//                     // 获取请求的查询参数，跳过第一个部分（即 "GetUserByParams"）
//                     var queryParams = new List<QueryParam>();
//                     for (int i = 1; i < requestParts.Length; i += 2)
//                     {
//                         string key = requestParts[i];
//                         string value = requestParts[i + 1];
//                         queryParams.Add(new QueryParam { Key = key, Value = value });
//                     }

//                     // 创建动态参数对象
//                     var dynamicParams = new DynamicParameters();
//                     foreach (var param in queryParams)
//                     {
//                         dynamicParams.Add(param.Key, param.Value);
//                     }

//                     // 调用 UserService 的 GetUserByParams 方法
//                     var user = _userService.GetUserByParams(dynamicParams);

//                     // 构建 ApiResponse 响应
//                     ApiResponse response = new ApiResponse();

//                     if (user != null)
//                     {
//                         response.Success = true;
//                         response.Message = "查询成功";
//                         response.User = user;  // 将 user 填充到 ApiResponse 的 user 字段中
//                     }
//                     else
//                     {
//                         response.Success = false;
//                         response.Message = "用户不存在";
//                         response.Error = "未找到符合条件的用户";
//                     }

//                     // 将响应发送给客户端
//                     SendResponse(response, stream);
//                 }
//                 else
//                 {
//                     var response = new ApiResponse
//                     {
//                         Message = "无效请求",
//                         Success = false,
//                         Error = "请求类型未识别或格式错误"
//                     };
//                     SendResponse(response, stream);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine("处理客户端请求时发生错误: " + e.Message);
//             }
//             finally
//             {
//                 stream.Close();
//                 client.Close();
//             }
//         }

//         private void SendResponse(object response, NetworkStream stream)
//         {
//             // 如果 response 是 Protobuf 消息对象，序列化它
//             byte[] responseBytes;
//             if (response is IMessage responseMessage)
//             {
//                 using (MemoryStream ms = new MemoryStream())
//                 {
//                     responseMessage.WriteTo(ms);
//                     responseBytes = ms.ToArray();
//                 }
//             }
//             else
//             {
//                 string str = response.ToString();
//                 if (!str.StartsWith("ERROR:") && !str.StartsWith("OK:"))
//                 {
//                     str = "ERROR:" + str;
//                 }
//                 responseBytes = Encoding.UTF8.GetBytes(str);
//             }

//             // 发送数据长度前缀
//             byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(responseBytes.Length));
//             stream.Write(lengthPrefix, 0, lengthPrefix.Length);  // 发送长度前缀
//             stream.Write(responseBytes, 0, responseBytes.Length);  // 发送 Protobuf 数据
//             Console.WriteLine("[UserController] 发送响应: " + response);
//         }
//     }
// }
