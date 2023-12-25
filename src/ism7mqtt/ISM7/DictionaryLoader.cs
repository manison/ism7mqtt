using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;

#nullable enable

namespace ism7mqtt
{
    internal sealed class DictionaryLoader : IDisposable
    {
        private readonly XmlReader _reader;

        private string _targetLanguage;

        public string TargetLanguage
        {
            get => _targetLanguage;
            [MemberNotNull(nameof(_targetLanguage))]
            set
            {
                CheckValidLanguage(value);
                _targetLanguage = value;
            }
        }

        private string _originalLanguage = "DEU";

        public string OriginalLanguage
        {
            get => _originalLanguage;
            set
            {
                CheckValidLanguage(value);
                _originalLanguage = value;
            }
        }

        public string? IncludeFile
        {
            get;
            set;
        }

        private Dictionary<string, string> _dictionary;
        public IReadOnlyDictionary<string, string> Dictionary => _dictionary;

        public DictionaryLoader(Stream stream, string language)
        {
            _reader = XmlReader.Create(stream);
            TargetLanguage = language;
            _dictionary = new Dictionary<string, string>();
        }

        public void Load()
        {
            if (_dictionary.Count > 0)
            {
                _dictionary = new Dictionary<string, string>();
            }

            if (_reader.ReadToFollowing("TextTableEntry"))
            {
                do
                {
                    LoadTableEntry();
                }
                while (_reader.ReadToFollowing("TextTableEntry"));
            }

            Validate();
        }

        private void LoadTableEntry()
        {
            string? key = null;
            string? value = null;
            string? file = null;

            _reader.ReadStartElement();
            while (_reader.MoveToContent() == XmlNodeType.Element)
            {
                if (_reader.LocalName == OriginalLanguage)
                {
                    key = _reader.ReadElementContentAsString();
                }
                else if (_reader.LocalName == TargetLanguage)
                {
                    value = _reader.ReadElementContentAsString();
                }
                else if (_reader.LocalName == "File")
                {
                    file = _reader.ReadElementContentAsString();
                }
                else
                {
                    _reader.Skip();
                }
            }

            if (key != null && value != null)
            {
                if (file == null || IncludeFile == null || file.Contains(IncludeFile))
                {
                    _dictionary[key] = value;
                }
            }
        }

        private void Validate()
        {
            if (Dictionary.Count == 0)
            {
                throw new ArgumentException($"No localization entry has been loaded for language {TargetLanguage}.");
            }
        }

        private static void CheckValidLanguage(string language)
        {
            if (String.IsNullOrEmpty(language) || language.Length != 3 ||
                !Char.IsUpper(language[0]) || !Char.IsUpper(language[1]) || !Char.IsUpper(language[2]))
            {
                throw new ArgumentException($"{language} is not valid language.");
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}
