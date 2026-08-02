using System;
using System.IO;
using System.Text;

namespace WorldIsMine.Net.Services
{
    public sealed class DyAnchorIdentity
    {
        public DyAnchorIdentity(string anchorId, string roomId)
            : this(anchorId, anchorId, roomId)
        {
        }

        public DyAnchorIdentity(string anchorId, string anchorName, string roomId)
        {
            AnchorId = anchorId?.Trim() ?? string.Empty;
            AnchorName = string.IsNullOrWhiteSpace(anchorName)
                ? AnchorId
                : anchorName.Trim();
            RoomId = roomId?.Trim() ?? string.Empty;
        }

        public string AnchorId { get; }
        public string AnchorName { get; }
        public string RoomId { get; }

        public void Validate()
        {
            ValidateValue(AnchorId, nameof(AnchorId));
            ValidateValue(AnchorName, nameof(AnchorName));
            ValidateValue(RoomId, nameof(RoomId));
        }

        private static void ValidateValue(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException($"{field} is required in the DY test identity markdown.");
            if (value.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                throw new InvalidDataException($"{field} must be a single line.");
        }
    }

    public static class DyTestIdentityStore
    {
        private const string AnchorIdKey = "AnchorId";
        private const string AnchorNameKey = "AnchorName";
        private const string RoomIdKey = "RoomId";

        public static DyAnchorIdentity Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Markdown path is required.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("DY test identity markdown was not found.", path);

            string anchorId = string.Empty;
            string anchorName = string.Empty;
            string roomId = string.Empty;
            foreach (string sourceLine in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = sourceLine.Trim().TrimStart('-', '*').Trim();
                int separator = line.IndexOf(':');
                if (separator <= 0)
                    continue;

                string key = line.Substring(0, separator).Trim();
                string value = NormalizeValue(line.Substring(separator + 1));
                if (key.Equals(AnchorIdKey, StringComparison.OrdinalIgnoreCase))
                    anchorId = value;
                else if (key.Equals(AnchorNameKey, StringComparison.OrdinalIgnoreCase))
                    anchorName = value;
                else if (key.Equals(RoomIdKey, StringComparison.OrdinalIgnoreCase))
                    roomId = value;
            }

            var identity = new DyAnchorIdentity(anchorId, anchorName, roomId);
            identity.Validate();
            return identity;
        }

        public static void Save(string path, DyAnchorIdentity identity)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Markdown path is required.", nameof(path));
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            identity.Validate();

            EnsureDirectory(path);
            string markdown =
                "# DY Test Identity" + Environment.NewLine +
                Environment.NewLine +
                $"{AnchorIdKey}: `{identity.AnchorId}`" + Environment.NewLine +
                $"{AnchorNameKey}: `{identity.AnchorName}`" + Environment.NewLine +
                $"{RoomIdKey}: `{identity.RoomId}`" + Environment.NewLine;
            File.WriteAllText(path, markdown, new UTF8Encoding(false));
        }

        public static void WriteTemplate(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Markdown path is required.", nameof(path));

            EnsureDirectory(path);
            string markdown =
                "# DY Test Identity" + Environment.NewLine +
                Environment.NewLine +
                "> Test mode only. Fill both values before connecting." + Environment.NewLine +
                Environment.NewLine +
                $"{AnchorIdKey}: ``" + Environment.NewLine +
                $"{AnchorNameKey}: ``" + Environment.NewLine +
                $"{RoomIdKey}: ``" + Environment.NewLine;
            File.WriteAllText(path, markdown, new UTF8Encoding(false));
        }

        private static string NormalizeValue(string source)
        {
            return source.Trim().Trim('`', '"', '\'').Trim();
        }

        private static void EnsureDirectory(string path)
        {
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
        }
    }
}
