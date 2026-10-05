using System.Collections.Generic;

namespace ChatPlus
{
    /// <summary>
    /// Messages you sent, for recalling them with the Up and Down arrows like in a terminal. Browsing starts only
    /// from an empty input field and stops as soon as the text is edited. Plain .NET, no Unity types.
    /// </summary>
    internal sealed class SentHistory
    {
        readonly List<string> _items = new List<string>();
        readonly int _capacity;
        // Index of the recalled message, -1 while not browsing.
        int _index = -1;
        // The text put into the input field by the last recall.
        string _shown;

        public SentHistory(int capacity)
        {
            _capacity = capacity < 1 ? 1 : capacity;
        }

        public int Count => _items.Count;

        public void Add(string text)
        {
            Reset();
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text.Trim()))
                return;
            if (_items.Count > 0 && _items[_items.Count - 1] == text)
                return;
            _items.Add(text);
            if (_items.Count > _capacity)
                _items.RemoveAt(0);
        }

        public void Reset()
        {
            _index = -1;
            _shown = null;
        }

        /// <summary>Text to put into the field after Up was pressed, or null to leave the field alone.</summary>
        public string Older(string current)
        {
            if (_items.Count == 0)
                return null;
            if (!Browsing(current))
            {
                if (!string.IsNullOrEmpty(current))
                    return null;
                _index = _items.Count;
            }
            if (_index == 0)
                return null;
            _index--;
            return _shown = _items[_index];
        }

        /// <summary>Text to put into the field after Down was pressed, or null to leave the field alone.</summary>
        public string Newer(string current)
        {
            if (!Browsing(current))
                return null;
            if (_index >= _items.Count - 1)
            {
                // Past the newest message: back to an empty field.
                Reset();
                return string.Empty;
            }
            _index++;
            return _shown = _items[_index];
        }

        bool Browsing(string current)
        {
            if (_index < 0)
                return false;
            if (current == _shown)
                return true;
            // The text was edited or sent: stop browsing.
            Reset();
            return false;
        }
    }
}
