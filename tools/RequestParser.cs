using Dapper;
using System.Globalization;

namespace GameServer.tools
{
    // 定义一个静态类 RequestParser，用于从请求字符串中解析各种数据类型
    public static class RequestParser
    {
        /// <summary>
        /// 从字符串数组中解析整数，如果解析失败则返回默认值
        /// </summary>
        /// <param name="parts">请求字符串数组</param>
        /// <param name="index">要解析的索引位置</param>
        /// <param name="defaultValue">解析失败时返回的默认值，默认为 0</param>
        /// <returns>解析出的整数或默认值</returns>
        public static int ParseInt(string[] parts, int index, int defaultValue = 0)
        {
            // 如果索引有效且成功解析为整数，则返回解析结果，否则返回默认值
            return (parts.Length > index && int.TryParse(parts[index], out int result)) ? result : defaultValue;
        }

        /// <summary>
        /// 从字符串数组中解析字符串，如果索引越界则返回默认值
        /// </summary>
        /// <param name="parts">请求字符串数组</param>
        /// <param name="index">要解析的索引位置</param>
        /// <param name="defaultValue">索引无效时返回的默认值，默认为空字符串</param>
        /// <returns>解析出的字符串或默认值</returns>
        public static string ParseString(string[] parts, int index, string defaultValue = "")
        {
            return parts.Length > index ? parts[index] : defaultValue;
        }

        /// <summary>
        /// 从字符串数组中解析布尔值，支持“0”表示 false，“1”表示 true
        /// </summary>
        /// <param name="parts">请求字符串数组</param>
        /// <param name="index">要解析的索引位置</param>
        /// <param name="isValid">输出参数，表示是否解析成功</param>
        /// <returns>返回解析出的布尔值（默认 false），isValid 表示是否为合法的 0/1</returns>
        public static bool ParseBoolFrom01(string[] parts, int index, out bool isValid)
        {
            isValid = false;
            if (parts.Length <= index) return false;
            if (parts[index] == "1") { isValid = true; return true; }
            if (parts[index] == "0") { isValid = true; return false; }
            return false;
        }

        /// <summary>
        /// 从字符串数组中解析 key=value 格式并自动推断类型，返回 DynamicParameters。
        /// 支持 int、bool(0/1)、double、DateTime、string。
        /// </summary>
        public static DynamicParameters ParseKeyValuePairsToParameters(string[] parts, int startIndex)
        {
            var parameters = new DynamicParameters();

            for (int i = startIndex; i < parts.Length; i++)
            {
                var part = parts[i];
                var pair = part.Split('=', 2);
                if (pair.Length != 2) continue;

                string key = pair[0].Trim();
                string value = pair[1].Trim();

                if (string.IsNullOrEmpty(key)) continue;

                // 类型推断顺序：int → bool → double → DateTime → string
                if (int.TryParse(value, out int intVal))
                {
                    parameters.Add(key, intVal);
                }
                else if ((key.ToLower().Contains("is") || key.ToLower().Contains("flag")) && (value == "0" || value == "1"))
                {
                    parameters.Add(key, value == "1");
                }
                else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleVal))
                {
                    parameters.Add(key, doubleVal);
                }
                else if (DateTime.TryParse(value, out DateTime dateVal))
                {
                    parameters.Add(key, dateVal);
                }
                else
                {
                    parameters.Add(key, value); // 默认字符串
                }
            }

            return parameters;
        }

    }
}