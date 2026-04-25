using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Tewi.Game.Console
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public ConsoleCommandAttribute(string name, string description = "")
        {
            Name = name.ToLower();
            Description = description;
        }
    }

    public class CommandProcessor
    {
        private struct CommandInfo
        {
            public MethodInfo Method;
            public object Target; // 如果是实例方法，存储对应的组件
            public string Description;
        }

        private Dictionary<string, CommandInfo> _commands = new Dictionary<string, CommandInfo>();

        public void ScanCommands()
        {
            _commands.Clear();
            // 查找场景中所有的 MonoBehaviour
            MonoBehaviour[] objects = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            foreach (var obj in objects)
            {
                // 提取所有带有 [ConsoleCommand] 特性的方法
                var methods = obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<ConsoleCommandAttribute>();
                    if (attr != null)
                    {
                        _commands[attr.Name] = new CommandInfo { Method = method, Target = obj, Description = attr.Description };
                    }
                }
            }
        }

        public string Execute(string inputLine)
        {
            string[] parts = inputLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "";

            string cmdName = parts[0].ToLower();
            if (cmdName == "help") return GetHelp();

            if (!_commands.TryGetValue(cmdName, out var cmd))
                return $"<color=red>未知命令: {cmdName}</color>";

            // 获取目标方法的参数信息
            ParameterInfo[] parameters = cmd.Method.GetParameters();
            object[] convertedArgs = new object[parameters.Length];

            // 智能参数匹配与转换
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i + 1 < parts.Length)
                {
                    try
                    {
                        convertedArgs[i] = ConvertParameter(parts[i + 1], parameters[i].ParameterType);
                    }
                    catch
                    {
                        return $"<color=red>参数错误: 第 {i + 1} 个参数应该是 {parameters[i].ParameterType.Name}</color>";
                    }
                }
                else if (parameters[i].HasDefaultValue)
                {
                    convertedArgs[i] = parameters[i].DefaultValue;
                }
                else
                {
                    return $"<color=red>缺少参数: {cmdName} 需要 {parameters.Length} 个参数</color>";
                }
            }

            try
            {
                object result = cmd.Method.Invoke(cmd.Target, convertedArgs);

                if (cmd.Method.ReturnType == typeof(string))
                {
                    return (string)result;
                }

                if (cmd.Method.ReturnType == typeof(void))
                {
                    return $"<color=green>执行完成: {cmdName}</color>";
                }

                return result?.ToString() ?? "";
            }
            catch (Exception e)
            {
                return $"<color=red>执行异常: {e.InnerException?.Message ?? e.Message}</color>";
            }
        }

        private string GetHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>可用命令列表:</b>");

            var sortedCommands = _commands.OrderBy(kvp => kvp.Key);
            foreach (var kvp in sortedCommands)
            {
                var pList = string.Join(" ", kvp.Value.Method.GetParameters().Select(p => $"[{p.Name}]"));
                sb.AppendLine($"- <color=yellow>{kvp.Key}</color> {pList} : {kvp.Value.Description}");
            }
            return sb.ToString();
        }

        private object ConvertParameter(string value, Type targetType)
        {
            // 处理基础类型 (int, float, bool, string, enum)
            if (targetType.IsPrimitive || targetType == typeof(string) || targetType.IsEnum)
            {
                // 专门处理布尔值，支持 1/0, true/false, on/off
                if (targetType == typeof(bool))
                {
                    if (value == "1" || value.ToLower() == "on" || value.ToLower() == "true") return true;
                    if (value == "0" || value.ToLower() == "off" || value.ToLower() == "false") return false;
                }
                return Convert.ChangeType(value, targetType);
            }

            // 处理 Quaternion (格式: x,y,z)
            if (targetType == typeof(Quaternion))
            {
                string[] s = value.Split(',');
                if (s.Length != 4) throw new Exception("Quaternion 格式应为 x,y,z,w");
                return new Quaternion(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
            }

            // 处理 Vector3 (格式: x,y,z)
            if (targetType == typeof(Vector3))
            {
                string[] s = value.Split(',');
                if (s.Length != 3) throw new Exception("Vector3 格式应为 x,y,z");
                return new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }

            // 处理 Vector2 (格式: x,y)
            if (targetType == typeof(Vector2))
            {
                string[] s = value.Split(',');
                if (s.Length != 2) throw new Exception("Vector2 格式应为 x,y");
                return new Vector2(float.Parse(s[0]), float.Parse(s[1]));
            }

            // 处理 Vector2Int (格式: x,y)
            if (targetType == typeof(Vector2Int))
            {
                string[] s = value.Split(',');
                if (s.Length != 2) throw new Exception("Vector2Int 格式应为 x,y");
                return new Vector2Int(int.Parse(s[0]), int.Parse(s[1]));
            }

            // 处理 Color (格式: r,g,b 或 r,g,b,a)
            if (targetType == typeof(Color))
            {
                string[] s = value.Split(',');
                if (s.Length == 3) return new Color(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
                if (s.Length == 4) return new Color(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
                throw new Exception("Color 格式应为 r,g,b 或 r,g,b,a");
            }

            throw new NotSupportedException($"不支持自动转换类型: {targetType.Name}");
        }
    }
}
