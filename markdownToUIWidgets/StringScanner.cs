using System;
using System.Text.RegularExpressions;
using markdown;
using UnityEngine;

namespace markdownToUIWidgets
{
    public class StringScanner
    {
        public static bool _slashAutoEscape = new Regex("/").ToString() == "\\/";


        string _source { get; set; }

        private int _position;

        public int position
        {
            get { return _position; }
            set
            {
                if (position < 0 || position > _source.Length)
                {
                    throw new ArgumentException($"Invalid position {position}");
                }

                _position = position;
                _lastMatch = null;
            }
        }

        /// The data about the previous match made by the scanner.
        ///
        /// If the last match failed, this will be `null`.
        public Match lastMatch
        {
            get
            {
                // Lazily unset [_lastMatch] so that we avoid extra assignments in
                // character-by-character methods that are used in core loops.
                if (_position != _lastMatchPosition) _lastMatch = null;
                return this._lastMatch;
            }
        }

        Match _lastMatch;
        int _lastMatchPosition;


        public string reset
        {
            get { return _source.substring(position); }
        }

        public bool isDone
        {
            get { return this.position >= this._source.Length; }
        }

        public StringScanner(string source, int position = -1)
        {
            this._source = source;
            if (position != -1)
                this.position = position;
        }

        /// Consumes a single character and returns its character code.
        ///
        /// This throws a [FormatException] if the string has been fully consumed. It
        /// doesn't affect [lastMatch].
        int readChar()
        {
            if (isDone)
                _fail("more input");

            return _source.codeUnitAt(_position++);
        }

        /// Returns the character code of the character [offset] away from [position].
        ///
        /// [offset] defaults to zero, and may be negative to inspect already-consumed
        /// characters.
        ///
        /// This returns `null` if [offset] points outside the string. It doesn't
        /// affect [lastMatch].
        int peekChar(int offset = -1)
        {
            if (offset == -1) offset = 0;
            var index = position + offset;
            if (index < 0 || index >= _source.Length) return 0;
            return _source.codeUnitAt(index);
        }


        /// If the next character in the string is [character], consumes it.
        ///
        /// Returns whether or not [character] was consumed.
        bool scanChar(int character)
        {
            if (isDone) return false;
            if (_source.codeUnitAt(_position) != character) return false;
            _position++;
            return true;
        }

        /// If the next character in the string is [character], consumes it.
        ///
        /// If [character] could not be consumed, throws a [FormatException]
        /// describing the position of the failure. [name] is used in this error as
        /// the expected name of the character being matched; if it's `null`, the
        /// character itself is used instead.
        void expectChar(int character, string name = null)
        {
            if (scanChar(character)) return;

            if (name == null)
            {
                if (character == markdown.CharCode.backslash)
                {
                    name = @"\";
                }
                else if (character == CharCode.quote)
                {
                    name = @"""";
                }
                else
                {
                    name = ((char) character).ToString();
                }
            }

            _fail(name);
        }

        public override string ToString()
        {
            return this.isDone ? "" : this._source.substring(this.position);
        }


        public bool scan(Regex regex)
        {
            var success = matches(regex);
            if (success)
            {
                _position = _lastMatch.Index + _lastMatch.Length;
                _lastMatchPosition = _position;
            }

            return success;
        }

        /// If [pattern] matches at the current position of the string, scans forward
        /// until the end of the match.
        ///
        /// If [pattern] did not match, throws a [FormatException] describing the
        /// position of the failure. [name] is used in this error as the expected name
        /// of the pattern being matched; if it's `null`, the pattern itself is used
        /// instead.
        void expect(Regex pattern, string name = null)
        {
            if (scan(pattern)) return;

            if (name == null)
            {
                var source = pattern.ToString();
                if (!_slashAutoEscape) source = source.replaceAll("/", "\\/");
                name = "/$source/";
            }

            _fail(name);
        }

        /// If the string has not been fully consumed, this throws a
        /// [FormatException].
        void expectDone()
        {
            if (isDone) return;
            _fail("no more input");
        }

        /// Returns whether or not [pattern] matches at the current position of the
        /// string.
        ///
        /// This doesn't move the scan pointer forward.
        bool matches(Regex pattern)
        {
            _lastMatch = pattern.matchAsPrefix(_source, position);
            _lastMatchPosition = _position;
            return _lastMatch.Success;
        }

        void _fail(String name)
        {
            Debug.LogErrorFormat("expected {0} -> {1}.", this.position, name);
        }
    }
}