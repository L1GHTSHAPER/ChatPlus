using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChatPlus
{
    /// <summary>
    /// Every chat line of this game session: kept in memory (to put it back into the chat after a lobby change) and
    /// written to chat-history.tsv, plus optional readable logs per day. Plain .NET, no Unity types.
    /// </summary>
    internal sealed class ChatHistory
    {
        public const string FileName = "chat-history.tsv";
        public const string PreviousFileName = "chat-history.previous.tsv";
        public const string LogFolderName = "logs";
        const string NewLine = "\r\n";

        // A byte order mark lets Excel and old editors detect UTF-8; it is written only when a file is created.
        static readonly Encoding Utf8Bom = new UTF8Encoding(true);
        static readonly Encoding Utf8 = new UTF8Encoding(false);

        readonly List<ChatEntry> _entries = new List<ChatEntry>();
        readonly int _capacity;
        readonly Action<string> _warn;
        bool _saving;
        // A write failed (e.g. the file was open in Excel): the next write puts the whole history in again.
        bool _fileBehind;
        string _lastError;

        public ChatHistory(string folder, int capacity, Action<string> warn)
        {
            Folder = folder;
            _capacity = Math.Max(1, capacity);
            _warn = warn;
        }

        public string Folder { get; }
        public string FilePath => Path.Combine(Folder, FileName);
        public string LogFolder => Path.Combine(Folder, LogFolderName);
        public int Count => _entries.Count;
        public bool Saving => _saving;
        /// <summary>Also append every line to logs/yyyy-MM-dd.txt.</summary>
        public bool DailyLogs { get; set; }

        /// <summary>
        /// Called once at startup. Either reads the history file of the previous game session, or moves it aside to
        /// chat-history.previous.tsv so that the new session starts empty.
        /// </summary>
        public void Open(bool keepPrevious, bool saving)
        {
            _saving = saving;
            if (keepPrevious)
                Load();
            else
                ArchivePrevious();
        }

        public void Add(ChatEntry entry)
        {
            if (entry == null)
                return;
            _entries.Add(entry);
            if (_entries.Count > _capacity)
                _entries.RemoveRange(0, _entries.Count - _capacity);
            if (_saving)
            {
                if (_fileBehind)
                    Rewrite();
                else
                    Append(entry);
            }
            if (DailyLogs)
                AppendDailyLog(entry);
        }

        /// <summary>A copy of the entries, oldest first.</summary>
        public List<ChatEntry> Snapshot()
        {
            return new List<ChatEntry>(_entries);
        }

        public void SetSaving(bool saving)
        {
            if (saving == _saving)
                return;
            _saving = saving;
            // The file missed everything said while saving was off.
            if (saving)
                Rewrite();
        }

        /// <summary>Forgets the session's history and empties the file.</summary>
        public void Clear()
        {
            _entries.Clear();
            if (_saving || File.Exists(FilePath))
                Rewrite();
        }

        void Load()
        {
            string path = FilePath;
            if (!File.Exists(path))
                return;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path, Utf8);
            }
            catch (Exception e)
            {
                Warn("Could not read the chat history: " + e.Message);
                return;
            }
            int records = 0;
            foreach (string line in lines)
            {
                if (line.Length == 0 || line.StartsWith("time\t", StringComparison.Ordinal))
                    continue;
                records++;
                if (HistoryFormat.TryParse(line, out ChatEntry entry))
                    _entries.Add(entry);
            }
            if (_entries.Count > _capacity)
                _entries.RemoveRange(0, _entries.Count - _capacity);
            // Drop what was cut off or unreadable, so the file does not grow forever across sessions.
            if (_saving && records != _entries.Count)
                Rewrite();
        }

        void ArchivePrevious()
        {
            string path = FilePath;
            try
            {
                if (!File.Exists(path))
                    return;
                string previous = Path.Combine(Folder, PreviousFileName);
                if (File.Exists(previous))
                    File.Delete(previous);
                File.Move(path, previous);
            }
            catch (Exception e)
            {
                Warn("Could not move the old chat history aside: " + e.Message);
                // Start over in place, so the old session is not appended to.
                Rewrite();
            }
        }

        void Append(ChatEntry entry)
        {
            try
            {
                string path = FilePath;
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(Folder);
                    File.WriteAllText(path, HistoryFormat.Header + NewLine, Utf8Bom);
                }
                File.AppendAllText(path, HistoryFormat.ToRecord(entry) + NewLine, Utf8);
                _lastError = null;
            }
            catch (Exception e)
            {
                _fileBehind = true;
                Warn("Could not write the chat history, will try again with the next message: " + e.Message);
            }
        }

        /// <summary>Writes the whole file again from memory.</summary>
        void Rewrite()
        {
            string path = FilePath;
            string temp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Folder);
                var text = new StringBuilder(HistoryFormat.Header.Length + 2 + _entries.Count * 96);
                text.Append(HistoryFormat.Header).Append(NewLine);
                foreach (ChatEntry entry in _entries)
                    text.Append(HistoryFormat.ToRecord(entry)).Append(NewLine);
                File.WriteAllText(temp, text.ToString(), Utf8Bom);
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(temp, path);
                _fileBehind = false;
                _lastError = null;
            }
            catch (Exception e)
            {
                _fileBehind = true;
                Warn("Could not write the chat history, will try again with the next message: " + e.Message);
                try
                {
                    if (File.Exists(temp))
                        File.Delete(temp);
                }
                catch (Exception)
                {
                    // Nothing more to do.
                }
            }
        }

        void AppendDailyLog(ChatEntry entry)
        {
            try
            {
                string folder = LogFolder;
                string path = Path.Combine(folder, entry.Time.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + ".txt");
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(folder);
                    File.WriteAllText(path, string.Empty, Utf8Bom);
                }
                File.AppendAllText(path, HistoryFormat.ToReadable(entry) + NewLine, Utf8);
            }
            catch (Exception e)
            {
                Warn("Could not write the chat log: " + e.Message);
            }
        }

        void Warn(string message)
        {
            // The same failure (e.g. a locked file) would repeat for every message.
            if (message == _lastError)
                return;
            _lastError = message;
            _warn?.Invoke(message);
        }
    }
}
