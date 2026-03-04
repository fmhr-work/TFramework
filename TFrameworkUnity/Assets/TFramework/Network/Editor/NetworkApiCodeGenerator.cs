using System;
using System.IO;
using System.Text;
using System.Net.Http;
using UnityEditor;
using UnityEngine;
using TFramework.Debug;
using Newtonsoft.Json.Linq;

namespace TFramework.Network.Editor
{
    /// <summary>
    /// API定義からC#コードを生成するロジッククラス
    /// </summary>
    public static class NetworkApiCodeGenerator
    {
        private const string GeneratedNamespace = "Game.Network.API.Generated";

        [MenuItem("TFramework/Network/Generate API Code", false, 100)]
        public static async void GenerateFromSettings()
        {
            var settings = NetworkSettings.Instance;
            if (settings == null)
            {
                TLogger.Error("[NetworkApiCodeGenerator] NetworkSettings instance not found.");
                return;
            }

            string schemaUrl = settings.SchemaUrl;
            if (string.IsNullOrEmpty(schemaUrl))
            {
                TLogger.Error($"[NetworkApiCodeGenerator] SchemaUrl is not configured for environment: {settings.CurrentEnvironment}");
                return;
            }

            TLogger.Info($"[NetworkApiCodeGenerator] Fetching schema from {schemaUrl} ...");

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    string schemaJson = await client.GetStringAsync(schemaUrl);
                    
                    if (string.IsNullOrEmpty(schemaJson))
                    {
                        TLogger.Error("[NetworkApiCodeGenerator] Downloaded schema JSON is empty.");
                        return;
                    }
                    
                    Generate(schemaJson, settings.ApiOutputPath);
                }
            }
            catch (Exception ex)
            {
                TLogger.Error($"[NetworkApiCodeGenerator] Failed to download or generate schema: {ex}");
            }
        }

        public static void Generate(string schemaJson, string relativeOutputPath)
        {
            if (string.IsNullOrEmpty(schemaJson))
            {
                TLogger.Error("[NetworkApiCodeGenerator] Schema JSON is empty.");
                return;
            }

            try
            {
                JObject schema = JObject.Parse(schemaJson);
                
                string absoluteOutputPath = Path.Combine(Application.dataPath, relativeOutputPath);
                
                string apiOutputPath = Path.Combine(absoluteOutputPath, "APIs");
                string modelOutputPath = Path.Combine(absoluteOutputPath, "Models");

                if (!Directory.Exists(apiOutputPath))
                {
                    Directory.CreateDirectory(apiOutputPath);
                }
                
                if (!Directory.Exists(modelOutputPath))
                {
                    Directory.CreateDirectory(modelOutputPath);
                }

                int apiCount = 0;
                int modelCount = 0;

                var definitions = schema["definitions"] as JObject;
                if (definitions != null)
                {
                    foreach (var def in definitions)
                    {
                        var modelName = SanitizeName(def.Key, true);
                        var props = def.Value["properties"] as JObject;
                        GenerateModelClass(modelName, props, modelOutputPath);
                        modelCount++;
                    }
                }

                var paths = schema["paths"] as JObject;
                if (paths != null)
                {
                    foreach (var path in paths)
                    {
                        string pathUrl = path.Key;
                        var methods = path.Value as JObject;
                        if (methods != null)
                        {
                            foreach (var method in methods)
                            {
                                string httpMethod = method.Key.ToUpper();
                                var operation = method.Value as JObject;
                                
                                string operationId = operation["operationId"]?.ToString();
                                string apiClassName = string.IsNullOrEmpty(operationId) 
                                    ? GenerateClassNameFromPath(httpMethod, pathUrl) 
                                    : SanitizeName(operationId, true);

                                string responseType = GetResponseType(operation);
                                var parameters = operation["parameters"] as JArray;

                                GenerateApiClass(apiClassName, pathUrl, httpMethod, responseType, parameters, apiOutputPath);
                                apiCount++;
                            }
                        }
                    }
                }
                
                TLogger.Info($"[NetworkApiCodeGenerator] Generated {apiCount} APIs and {modelCount} Models.");
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                TLogger.Error($"[NetworkApiCodeGenerator] Failed to parse schema: {ex.Message}");
            }
        }

        private static void GenerateApiClass(string className, string url, string method, string responseType, JArray parameters, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// THIS CODE IS GENERATED BY TFRAMEWORK API CODE GENERATOR.");
            sb.AppendLine("// DO NOT EDIT THIS FILE DIRECTLY.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using TFramework.Network;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {GeneratedNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className} : ApiBase<{className}, {className}.Request, {responseType}>");
            sb.AppendLine("    {");
            sb.AppendLine("        public class Request : RequestBase");
            sb.AppendLine("        {");
            sb.AppendLine($"            public override string Name => \"{url}\";");
            sb.AppendLine($"            public override ApiType Type => ApiType.{method};");
            sb.AppendLine();
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    string paramName = SanitizeName(param["name"]?.ToString(), false);
                    string paramType = ResolveTypeFromToken(param); 
                    
                    if (string.IsNullOrEmpty(paramName)) continue;
                    
                    sb.AppendLine($"            public {paramType} {paramName} {{ get; set; }}");
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(outputPath, $"{className}.cs"), sb.ToString());
        }

        private static void GenerateModelClass(string className, JObject properties, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// THIS CODE IS GENERATED BY TFRAMEWORK API CODE GENERATOR.");
            sb.AppendLine("// DO NOT EDIT THIS FILE DIRECTLY.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using TFramework.Network;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {GeneratedNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    [Serializable]");
            sb.AppendLine($"    public class {className} : ResponseBase");
            sb.AppendLine("    {");

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    string fieldName = SanitizeName(prop.Key, false);
                    string fieldType = ResolveTypeFromToken(prop.Value);
                    sb.AppendLine($"        public {fieldType} {fieldName};");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(outputPath, $"{className}.cs"), sb.ToString());
        }

        private static string GetResponseType(JObject operation)
        {
            var responses = operation["responses"] as JObject;
            if (responses != null)
            {
                var okResponse = responses["200"];
                if (okResponse != null)
                {
                    var schema = okResponse["schema"] as JToken;
                    if (schema != null)
                    {
                        var resolved = ResolveTypeFromToken(schema);
                        return resolved == "object" ? "ResponseBase" : resolved;
                    }
                }
            }
            return "ResponseBase"; // default
        }

        private static string ResolveTypeFromToken(JToken token)
        {
            if (token == null) return "string";

            var refToken = token["schema"]?["$ref"] ?? token["$ref"];
            if (refToken != null)
            {
                string refPath = refToken.ToString();
                return SanitizeName(refPath.Substring(refPath.LastIndexOf('/') + 1), true);
            }

            var schemaToken = token["schema"];
            if (schemaToken != null) 
            {
                return ResolveTypeFromToken(schemaToken);
            }

            string typeToken = token["type"]?.ToString();
            
            if (typeToken == "array")
            {
                var items = token["items"];
                string itemType = ResolveTypeFromToken(items);
                return $"List<{itemType}>";
            }

            string formatToken = token["format"]?.ToString();
            
            switch (typeToken)
            {
                case "integer": return formatToken == "int64" ? "long" : "int";
                case "number": return formatToken == "float" ? "float" : "double";
                case "boolean": return "bool";
                case "string": return "string";
                case "object": return "object";
                default: return "string";
            }
        }

        private static string SanitizeName(string name, bool isClass = false)
        {
            if (string.IsNullOrEmpty(name)) return "Unknown";
            
            // Invalid char除去
            name = name.Replace("-", "").Replace(".", "").Replace("[", "").Replace("]", "");
            
            if (isClass && char.IsLower(name[0]))
            {
                name = char.ToUpper(name[0]) + name.Substring(1);
            }
            return name;
        }

        private static string GenerateClassNameFromPath(string method, string path)
        {
            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string name = method.ToLower();
            foreach (var part in parts)
            {
                var cleanPart = part.Replace("{", "").Replace("}", "");
                if (cleanPart.Length > 0)
                {
                    name += char.ToUpper(cleanPart[0]) + cleanPart.Substring(1);
                }
            }
            return SanitizeName(name, true);
        }
    }
}
