using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public static class ServerConfigExporter
{
    public static readonly string OutputRootPath = Path.GetFullPath(
        Path.Combine(Application.dataPath, "../cfg_table"));

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly Regex IdentifierPattern = new Regex(
        "^[A-Za-z_][A-Za-z0-9_]*$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> SupportedTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "bool", "int", "uint", "long", "ulong", "float", "double", "decimal", "string",
        "bool[]", "int[]", "uint[]", "long[]", "ulong[]", "float[]", "double[]", "decimal[]", "string[]"
    };

    private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while"
    };

    public static void ExportAll()
    {
        string stagingPath = OutputRootPath + ".__exporting";
        string backupPath = OutputRootPath + ".__backup";

        RecoverInterruptedPublish(backupPath);
        DeleteDirectory(stagingPath);
        Directory.CreateDirectory(Path.Combine(stagingPath, "json"));

        try
        {
            List<ServerSheet> sheets = ReadAllSheets();
            if (sheets.Count == 0)
                throw new InvalidDataException("没有找到可导出的服务器配置表");

            WriteText(Path.Combine(stagingPath, "ConfigTable.g.cs"), BuildConfigTableRuntime());

            var manifestFiles = new List<ManifestFile>();
            foreach (ServerSheet sheet in sheets)
            {
                WriteText(
                    Path.Combine(stagingPath, sheet.Name + ".g.cs"),
                    BuildSheetCode(sheet));

                string relativeJsonPath = "json/" + sheet.Name + ".json";
                string json = JsonConvert.SerializeObject(sheet.Rows, Formatting.None);
                string jsonPath = Path.Combine(stagingPath, "json", sheet.Name + ".json");
                WriteText(jsonPath, json);
                manifestFiles.Add(new ManifestFile(relativeJsonPath, Sha256File(jsonPath)));
            }

            manifestFiles.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.path, right.path));

            string version = ComputeVersion(manifestFiles);
            WriteText(
                Path.Combine(stagingPath, "GameConfigs.g.cs"),
                BuildGameConfigsCode(sheets));
            WriteText(
                Path.Combine(stagingPath, "manifest.json"),
                JsonConvert.SerializeObject(
                    new Manifest(version, manifestFiles),
                    Formatting.Indented));

            Publish(stagingPath, backupPath);
            Debug.Log("服务器配置导出成功，表数量=" + sheets.Count + "，版本=" + version);
        }
        catch
        {
            DeleteDirectory(stagingPath);
            throw;
        }
    }

    private static List<ServerSheet> ReadAllSheets()
    {
        string[] excelFiles = Directory.GetFiles(
            ExportExcel.ImportExcelPath,
            "*.xlsx",
            SearchOption.TopDirectoryOnly);
        Array.Sort(excelFiles, StringComparer.Ordinal);

        var result = new List<ServerSheet>();
        var sheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string excelFile in excelFiles)
        {
            if (Path.GetFileName(excelFile).StartsWith("~$", StringComparison.Ordinal))
                continue;

            using (var package = new ExcelPackage(new FileInfo(excelFile)))
            {
                for (int i = 1; i <= package.Workbook.Worksheets.Count; i++)
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[i];
                    ServerSheet sheet = ReadSheet(worksheet, Path.GetFileName(excelFile));

                    if (!sheetNames.Add(sheet.Name))
                        throw new InvalidDataException("存在重复Sheet名称: " + sheet.Name);

                    if (!propertyNames.Add(sheet.PropertyName))
                    {
                        throw new InvalidDataException(
                            "配置总入口属性名称冲突: " + sheet.PropertyName + "，Sheet=" + sheet.Name);
                    }

                    result.Add(sheet);
                }
            }
        }

        result.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.Name, right.Name));
        return result;
    }

    private static ServerSheet ReadSheet(ExcelWorksheet sheet, string excelName)
    {
        string context = excelName + "/" + sheet.Name;
        ValidateIdentifier(sheet.Name, "Sheet名称", context);

        if (sheet.Dimension == null || sheet.Dimension.Rows < 2)
            throw new InvalidDataException(context + " 是空表或缺少字段定义");

        int rowCount = sheet.Dimension.Rows;
        int columnCount = sheet.Dimension.Columns;
        var columns = new List<ServerColumn>(columnCount);
        var columnNames = new HashSet<string>(StringComparer.Ordinal);

        for (int column = 1; column <= columnCount; column++)
        {
            string name = Convert.ToString(
                sheet.Cells[1, column].Value,
                CultureInfo.InvariantCulture);
            string type = Convert.ToString(
                sheet.Cells[2, column].Value,
                CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidDataException(context + " 第1行第" + column + "列字段名为空");
            if (string.IsNullOrWhiteSpace(type))
                throw new InvalidDataException(context + " 第2行第" + column + "列字段类型为空");

            name = name.Trim();
            type = type.Trim();
            ValidateIdentifier(name, "字段名", context);

            if (!columnNames.Add(name))
                throw new InvalidDataException(context + " 存在重复字段: " + name);
            if (!SupportedTypes.Contains(type))
                throw new InvalidDataException(context + " 字段 " + name + " 使用了不支持的类型: " + type);

            columns.Add(new ServerColumn(name, type, column));
        }

        ServerColumn idColumn = columns.FirstOrDefault(
            column => string.Equals(column.Name, "id", StringComparison.OrdinalIgnoreCase));
        if (idColumn == null)
            throw new InvalidDataException(context + " 缺少id字段，服务器配置必须使用id作为主键");
        if (!IsSupportedIdType(idColumn.Type))
            throw new InvalidDataException(context + " 的id类型只支持int、uint、long、ulong或string");

        var rows = new List<Dictionary<string, object>>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (int row = 4; row <= rowCount; row++)
        {
            if (IsEmptyRow(sheet, row, columnCount))
                continue;

            var data = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (ServerColumn column in columns)
            {
                object raw = sheet.Cells[row, column.Index].Value;
                object value = ConvertValue(raw, column.Type, context, row, column.Index);
                data.Add(column.Name, value);
            }

            string idKey = idColumn.Type + ":" +
                           JsonConvert.SerializeObject(data[idColumn.Name], Formatting.None);
            if (!ids.Add(idKey))
                throw new InvalidDataException(context + " 第" + row + "行存在重复id: " + data[idColumn.Name]);

            rows.Add(data);
        }

        string propertyName = ToPascalCase(sheet.Name);
        if (string.IsNullOrEmpty(propertyName))
            throw new InvalidDataException(context + " 无法生成配置总入口属性名");

        return new ServerSheet(
            sheet.Name,
            propertyName,
            columns,
            idColumn,
            rows);
    }

    private static bool IsEmptyRow(ExcelWorksheet sheet, int row, int columnCount)
    {
        for (int column = 1; column <= columnCount; column++)
        {
            if (sheet.Cells[row, column].Value != null)
                return false;
        }
        return true;
    }

    private static object ConvertValue(
        object raw,
        string type,
        string context,
        int row,
        int column)
    {
        if (raw == null)
        {
            if (type == "string")
                return string.Empty;
            if (type.EndsWith("[]", StringComparison.Ordinal))
                return Array.CreateInstance(GetArrayElementType(type), 0);

            throw new InvalidDataException(
                context + " 第" + row + "行第" + column + "列不能为空，类型=" + type);
        }

        string text = Convert.ToString(raw, CultureInfo.InvariantCulture).Trim();
        if (type.EndsWith("[]", StringComparison.Ordinal))
            return ConvertArray(text, type, context, row, column);

        try
        {
            switch (type)
            {
                case "string": return text;
                case "bool": return ParseBool(text);
                case "int": return int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "uint": return uint.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "long": return long.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "ulong": return ulong.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "float": return float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
                case "double": return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
                case "decimal": return decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);
                default: throw new InvalidDataException("不支持的配置类型: " + type);
            }
        }
        catch (Exception ex) when (!(ex is InvalidDataException))
        {
            throw new InvalidDataException(
                context + " 第" + row + "行第" + column + "列无法转换为" + type + ": " + text,
                ex);
        }
    }

    private static object ConvertArray(
        string text,
        string type,
        string context,
        int row,
        int column)
    {
        string elementType = type.Substring(0, type.Length - 2);
        if (string.IsNullOrWhiteSpace(text))
            return Array.CreateInstance(GetArrayElementType(type), 0);

        string[] values = text.Split(',');
        Array result = Array.CreateInstance(GetArrayElementType(type), values.Length);
        for (int i = 0; i < values.Length; i++)
        {
            result.SetValue(
                ConvertValue(values[i].Trim(), elementType, context, row, column),
                i);
        }
        return result;
    }

    private static Type GetArrayElementType(string type)
    {
        switch (type.Substring(0, type.Length - 2))
        {
            case "string": return typeof(string);
            case "bool": return typeof(bool);
            case "int": return typeof(int);
            case "uint": return typeof(uint);
            case "long": return typeof(long);
            case "ulong": return typeof(ulong);
            case "float": return typeof(float);
            case "double": return typeof(double);
            case "decimal": return typeof(decimal);
            default: throw new InvalidDataException("不支持的数组类型: " + type);
        }
    }

    private static bool ParseBool(string value)
    {
        if (string.Equals(value, "1", StringComparison.Ordinal))
            return true;
        if (string.Equals(value, "0", StringComparison.Ordinal))
            return false;
        return bool.Parse(value);
    }

    private static bool IsSupportedIdType(string type)
    {
        return type == "int" || type == "uint" || type == "long" ||
               type == "ulong" || type == "string";
    }

    private static void ValidateIdentifier(string value, string label, string context)
    {
        if (!IdentifierPattern.IsMatch(value) || CSharpKeywords.Contains(value))
            throw new InvalidDataException(context + " 的" + label + "不是合法C#标识符: " + value);
    }

    private static string BuildSheetCode(ServerSheet sheet)
    {
        var code = new StringBuilder();
        code.AppendLine("#nullable enable");
        code.AppendLine("using System;");
        code.AppendLine("using System.Collections.Generic;");
        code.AppendLine();
        code.AppendLine("namespace GameConfig.Generated");
        code.AppendLine("{");
        code.Append("    public sealed class ").Append(sheet.Name).AppendLine();
        code.AppendLine("    {");

        foreach (ServerColumn column in sheet.Columns)
        {
            code.Append("        public ").Append(column.Type).Append(' ').Append(column.Name)
                .Append(" { get; set; }").Append(GetDefaultInitializer(column.Type)).AppendLine();
        }

        code.AppendLine("    }");
        code.AppendLine();
        code.Append("    public sealed class ").Append(sheet.Name)
            .Append("Table : ConfigTable<").Append(sheet.IdColumn.Type).Append(", ")
            .Append(sheet.Name).AppendLine(">");
        code.AppendLine("    {");
        code.Append("        internal ").Append(sheet.Name).Append("Table(IReadOnlyList<")
            .Append(sheet.Name).AppendLine("> rows)");
        code.Append("            : base(rows, row => row.").Append(sheet.IdColumn.Name).AppendLine(")");
        code.AppendLine("        {");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("}");
        return code.ToString();
    }

    private static string GetDefaultInitializer(string type)
    {
        if (type == "string")
            return " = string.Empty;";
        if (type.EndsWith("[]", StringComparison.Ordinal))
            return " = Array.Empty<" + type.Substring(0, type.Length - 2) + ">();";
        return string.Empty;
    }

    private static string BuildConfigTableRuntime()
    {
        return @"#nullable enable
using System;
using System.Collections.Generic;

namespace GameConfig.Generated
{
    public abstract class ConfigTable<TKey, TRow> where TKey : notnull
    {
        private readonly Dictionary<TKey, TRow> _byId;

        protected ConfigTable(IReadOnlyList<TRow> rows, Func<TRow, TKey> getId)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (getId == null) throw new ArgumentNullException(nameof(getId));

            TRow[] snapshot = new TRow[rows.Count];
            _byId = new Dictionary<TKey, TRow>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                TRow row = rows[i];
                TKey id = getId(row);
                if (!_byId.TryAdd(id, row))
                {
                    throw new InvalidOperationException(
                        $""duplicate_config_id table={typeof(TRow).Name} id={id}"");
                }
                snapshot[i] = row;
            }

            Rows = Array.AsReadOnly(snapshot);
        }

        public IReadOnlyList<TRow> Rows { get; }
        public int Count => Rows.Count;

        public bool TryGet(TKey id, out TRow value) => _byId.TryGetValue(id, out value!);

        public TRow GetRequired(TKey id) =>
            _byId.TryGetValue(id, out TRow? value)
                ? value
                : throw new KeyNotFoundException(
                    $""config_not_found table={typeof(TRow).Name} id={id}"");
    }
}
";
    }

    private static string BuildGameConfigsCode(List<ServerSheet> sheets)
    {
        var code = new StringBuilder();
        code.AppendLine("#nullable enable");
        code.AppendLine("using System;");
        code.AppendLine("using System.Collections.Generic;");
        code.AppendLine("using System.IO;");
        code.AppendLine("using System.Linq;");
        code.AppendLine("using System.Security.Cryptography;");
        code.AppendLine("using System.Text;");
        code.AppendLine("using System.Text.Json;");
        code.AppendLine("using System.Threading;");
        code.AppendLine();
        code.AppendLine("namespace GameConfig.Generated");
        code.AppendLine("{");
        code.AppendLine("    public sealed class GameConfigSnapshot");
        code.AppendLine("    {");

        foreach (ServerSheet sheet in sheets)
        {
            code.Append("        public ").Append(sheet.Name).Append("Table ")
                .Append(sheet.PropertyName).AppendLine(" { get; }");
        }
        code.AppendLine("        public string Version { get; }");
        code.AppendLine();
        code.Append("        private GameConfigSnapshot(string version");
        foreach (ServerSheet sheet in sheets)
        {
            code.Append(", ").Append(sheet.Name).Append("Table ")
                .Append(ToCamelCase(sheet.PropertyName));
        }
        code.AppendLine(")");
        code.AppendLine("        {");
        code.AppendLine("            Version = version;");
        foreach (ServerSheet sheet in sheets)
        {
            code.Append("            ").Append(sheet.PropertyName).Append(" = ")
                .Append(ToCamelCase(sheet.PropertyName)).AppendLine(";");
        }
        code.AppendLine("        }");
        code.AppendLine();
        code.AppendLine("        public static GameConfigSnapshot Load(string rootDirectory)");
        code.AppendLine("        {");
        code.AppendLine("            string root = Path.GetFullPath(rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory)));");
        code.AppendLine("            string manifestPath = ResolvePath(root, \"manifest.json\");");
        code.AppendLine("            ConfigManifest? manifest = JsonSerializer.Deserialize<ConfigManifest>(File.ReadAllText(manifestPath));");
        code.AppendLine("            if (manifest == null) throw new InvalidDataException(\"invalid_config_manifest\");");
        code.AppendLine("            VerifyManifest(root, manifest);");
        code.AppendLine();
        code.AppendLine("            return new GameConfigSnapshot(");
        code.AppendLine("                manifest.version,");
        for (int i = 0; i < sheets.Count; i++)
        {
            ServerSheet sheet = sheets[i];
            string suffix = i == sheets.Count - 1 ? ");" : ",";
            code.Append("                new ").Append(sheet.Name).Append("Table(LoadRows<")
                .Append(sheet.Name).Append(">(root, \"json/").Append(sheet.Name)
                .Append(".json\"))").AppendLine(suffix);
        }
        code.AppendLine("        }");
        code.AppendLine();
        AppendGeneratedLoaderHelpers(code, sheets);
        code.AppendLine("    }");
        code.AppendLine();
        code.AppendLine("    public static class GameConfigs");
        code.AppendLine("    {");
        code.AppendLine("        private static GameConfigSnapshot? _current;");
        code.AppendLine();
        code.AppendLine("        public static GameConfigSnapshot Current =>");
        code.AppendLine("            Volatile.Read(ref _current) ??");
        code.AppendLine("            throw new InvalidOperationException(\"game_config_not_loaded\");");
        code.AppendLine();
        code.AppendLine("        public static void Load(string rootDirectory, Action<GameConfigSnapshot>? validate = null)");
        code.AppendLine("        {");
        code.AppendLine("            GameConfigSnapshot next = GameConfigSnapshot.Load(rootDirectory);");
        code.AppendLine("            validate?.Invoke(next);");
        code.AppendLine("            Interlocked.Exchange(ref _current, next);");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine();
        code.AppendLine("    internal sealed class ConfigManifest");
        code.AppendLine("    {");
        code.AppendLine("        public ConfigManifest() { }");
        code.AppendLine("        public string version { get; set; } = string.Empty;");
        code.AppendLine("        public List<ConfigManifestFile> files { get; set; } = new List<ConfigManifestFile>();");
        code.AppendLine("    }");
        code.AppendLine();
        code.AppendLine("    internal sealed class ConfigManifestFile");
        code.AppendLine("    {");
        code.AppendLine("        public ConfigManifestFile() { }");
        code.AppendLine("        public string path { get; set; } = string.Empty;");
        code.AppendLine("        public string sha256 { get; set; } = string.Empty;");
        code.AppendLine("    }");
        code.AppendLine("}");
        return code.ToString();
    }

    private static void AppendGeneratedLoaderHelpers(StringBuilder code, List<ServerSheet> sheets)
    {
        code.AppendLine("        private static readonly string[] ExpectedFiles =");
        code.AppendLine("        {");
        foreach (ServerSheet sheet in sheets)
            code.Append("            \"json/").Append(sheet.Name).AppendLine(".json\",");
        code.AppendLine("        };");
        code.AppendLine();
        code.AppendLine("        private static List<T> LoadRows<T>(string root, string relativePath)");
        code.AppendLine("        {");
        code.AppendLine("            string json = File.ReadAllText(ResolvePath(root, relativePath));");
        code.AppendLine("            return JsonSerializer.Deserialize<List<T>>(json) ??");
        code.AppendLine("                   throw new InvalidDataException($\"invalid_config_json file={relativePath}\");");
        code.AppendLine("        }");
        code.AppendLine();
        code.AppendLine("        private static void VerifyManifest(string root, ConfigManifest manifest)");
        code.AppendLine("        {");
        code.AppendLine("            var expected = new HashSet<string>(ExpectedFiles, StringComparer.Ordinal);");
        code.AppendLine("            var actual = new HashSet<string>(StringComparer.Ordinal);");
        code.AppendLine("            foreach (ConfigManifestFile file in manifest.files)");
        code.AppendLine("            {");
        code.AppendLine("                if (!actual.Add(file.path)) throw new InvalidDataException($\"duplicate_manifest_file file={file.path}\");");
        code.AppendLine("                if (!expected.Contains(file.path)) throw new InvalidDataException($\"unexpected_manifest_file file={file.path}\");");
        code.AppendLine("                string hash = Sha256File(ResolvePath(root, file.path));");
        code.AppendLine("                if (!string.Equals(hash, file.sha256, StringComparison.OrdinalIgnoreCase))");
        code.AppendLine("                    throw new InvalidDataException($\"config_hash_mismatch file={file.path}\");");
        code.AppendLine("            }");
        code.AppendLine("            if (!expected.SetEquals(actual)) throw new InvalidDataException(\"config_manifest_files_mismatch\");");
        code.AppendLine("            string version = ComputeVersion(manifest.files);");
        code.AppendLine("            if (!string.Equals(version, manifest.version, StringComparison.OrdinalIgnoreCase))");
        code.AppendLine("                throw new InvalidDataException(\"config_version_mismatch\");");
        code.AppendLine("        }");
        code.AppendLine();
        code.AppendLine("        private static string ResolvePath(string root, string relativePath)");
        code.AppendLine("        {");
        code.AppendLine("            string fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));");
        code.AppendLine("            string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;");
        code.AppendLine("            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))");
        code.AppendLine("                throw new InvalidDataException($\"config_path_outside_root path={relativePath}\");");
        code.AppendLine("            return fullPath;");
        code.AppendLine("        }");
        code.AppendLine();
        code.AppendLine("        private static string Sha256File(string path)");
        code.AppendLine("        {");
        code.AppendLine("            using SHA256 sha = SHA256.Create();");
        code.AppendLine("            using FileStream stream = File.OpenRead(path);");
        code.AppendLine("            return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();");
        code.AppendLine("        }");
        code.AppendLine();
        code.AppendLine("        private static string ComputeVersion(IEnumerable<ConfigManifestFile> files)");
        code.AppendLine("        {");
        code.AppendLine("            string input = string.Join(\"\\n\", files.OrderBy(file => file.path, StringComparer.Ordinal).Select(file => file.path + \":\" + file.sha256.ToLowerInvariant()));");
        code.AppendLine("            using SHA256 sha = SHA256.Create();");
        code.AppendLine("            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();");
        code.AppendLine("        }");
    }

    private static string ToPascalCase(string value)
    {
        var result = new StringBuilder(value.Length);
        bool upperNext = true;
        foreach (char character in value)
        {
            if (character == '_')
            {
                upperNext = true;
                continue;
            }

            result.Append(upperNext ? char.ToUpperInvariant(character) : character);
            upperNext = false;
        }
        return result.ToString();
    }

    private static string ToCamelCase(string value)
    {
        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    private static string ComputeVersion(List<ManifestFile> files)
    {
        string input = string.Join(
            "\n",
            files.OrderBy(file => file.path, StringComparer.Ordinal)
                .Select(file => file.path + ":" + file.sha256.ToLowerInvariant()));
        return Sha256Bytes(Utf8NoBom.GetBytes(input));
    }

    private static string Sha256File(string path)
    {
        using (SHA256 sha = SHA256.Create())
        using (FileStream stream = File.OpenRead(path))
            return ToHex(sha.ComputeHash(stream));
    }

    private static string Sha256Bytes(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return ToHex(sha.ComputeHash(bytes));
    }

    private static string ToHex(byte[] bytes)
    {
        var result = new StringBuilder(bytes.Length * 2);
        foreach (byte value in bytes)
            result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return result.ToString();
    }

    private static void WriteText(string path, string content)
    {
        File.WriteAllText(path, content, Utf8NoBom);
    }

    private static void RecoverInterruptedPublish(string backupPath)
    {
        if (!Directory.Exists(backupPath))
            return;

        if (!Directory.Exists(OutputRootPath))
            Directory.Move(backupPath, OutputRootPath);
        else
            DeleteDirectory(backupPath);
    }

    private static void Publish(string stagingPath, string backupPath)
    {
        bool oldMoved = false;
        try
        {
            if (Directory.Exists(OutputRootPath))
            {
                Directory.Move(OutputRootPath, backupPath);
                oldMoved = true;
            }

            Directory.Move(stagingPath, OutputRootPath);
            DeleteDirectory(backupPath);
        }
        catch
        {
            if (!Directory.Exists(OutputRootPath) && oldMoved && Directory.Exists(backupPath))
                Directory.Move(backupPath, OutputRootPath);
            throw;
        }
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    private sealed class ServerSheet
    {
        public ServerSheet(
            string name,
            string propertyName,
            List<ServerColumn> columns,
            ServerColumn idColumn,
            List<Dictionary<string, object>> rows)
        {
            Name = name;
            PropertyName = propertyName;
            Columns = columns;
            IdColumn = idColumn;
            Rows = rows;
        }

        public string Name { get; }
        public string PropertyName { get; }
        public List<ServerColumn> Columns { get; }
        public ServerColumn IdColumn { get; }
        public List<Dictionary<string, object>> Rows { get; }
    }

    private sealed class ServerColumn
    {
        public ServerColumn(string name, string type, int index)
        {
            Name = name;
            Type = type;
            Index = index;
        }

        public string Name { get; }
        public string Type { get; }
        public int Index { get; }
    }

    private sealed class Manifest
    {
        public Manifest(string version, List<ManifestFile> files)
        {
            this.version = version;
            this.files = files;
        }

        public string version { get; }
        public List<ManifestFile> files { get; }
    }

    private sealed class ManifestFile
    {
        public ManifestFile(string path, string sha256)
        {
            this.path = path;
            this.sha256 = sha256;
        }

        public string path { get; }
        public string sha256 { get; }
    }
}
