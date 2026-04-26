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
                return $"<color=red>Command not found: {cmdName}</color>";

            ParameterInfo[] parameters = cmd.Method.GetParameters();
            object[] convertedArgs = new object[parameters.Length];
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
                        return $"<color=red>Parameter error: Argument {i + 1} should be of type {parameters[i].ParameterType.Name}</color>";
                    }
                }
                else if (parameters[i].HasDefaultValue)
                {
                    convertedArgs[i] = parameters[i].DefaultValue;
                }
                else
                {
                    return $"<color=red>Missing parameter: {cmdName} requires {parameters.Length} arguments</color>";
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
                    return $"<color=green>Execution completed: {cmdName}</color>";
                }

                return result?.ToString() ?? "";
            }
            catch (Exception e)
            {
                return $"<color=red>Execution exception: {e.InnerException?.Message ?? e.Message}</color>";
            }
        }

        private string GetHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Available Commands:</b>");

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
            if (targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, value, true);
                }
                catch
                {
                    string names = string.Join(", ", Enum.GetNames(targetType));
                    throw new Exception($"'{value}' is not a valid {targetType.Name}. Available options: {names}");
                }
            }

            // Handle basic types (int, float, bool, string, enum)
            if (targetType.IsPrimitive || targetType == typeof(string))
            {
                // Special handling for boolean values, supporting 1/0, true/false, on/off
                if (targetType == typeof(bool))
                {
                    if (value == "1" || value.ToLower() == "on" || value.ToLower() == "true") return true;
                    if (value == "0" || value.ToLower() == "off" || value.ToLower() == "false") return false;
                }
                return Convert.ChangeType(value, targetType);
            }

            // Handle Quaternion (format: x,y,z,w or x,y,z)
            if (targetType == typeof(Quaternion))
            {
                string[] s = value.Split(',');
                if (s.Length == 4) return new Quaternion(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
                if (s.Length == 3) return Quaternion.Euler(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
                throw new Exception("Quaternion format should be x,y,z,w or x,y,z (Euler angles)");
            }

            // Handle Vector3 (format: x,y,z)
            if (targetType == typeof(Vector3))
            {
                string[] s = value.Split(',');
                if (s.Length != 3) throw new Exception("Vector3 format should be x,y,z");
                return new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }

            // Handle Vector2 (format: x,y)
            if (targetType == typeof(Vector2))
            {
                string[] s = value.Split(',');
                if (s.Length != 2) throw new Exception("Vector2 format should be x,y");
                return new Vector2(float.Parse(s[0]), float.Parse(s[1]));
            }

            // Handle Vector2Int (format: x,y)
            if (targetType == typeof(Vector2Int))
            {
                string[] s = value.Split(',');
                if (s.Length != 2) throw new Exception("Vector2Int format should be x,y");
                return new Vector2Int(int.Parse(s[0]), int.Parse(s[1]));
            }

            // Handle Color (format: r,g,b or r,g,b,a)
            if (targetType == typeof(Color))
            {
                string[] s = value.Split(',');
                if (s.Length == 3) return new Color(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
                if (s.Length == 4) return new Color(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
                throw new Exception("Color format should be r,g,b or r,g,b,a");
            }

            throw new NotSupportedException($"Automatic conversion not supported for type: {targetType.Name}");
        }
    }
}
