﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

using DArvis.Extensions;

namespace DArvis.IO
{
    public sealed class FileArchive : IDisposable
    {
        private const int NameLength = 13;

        private bool isDisposed;
        private readonly Dictionary<string, FileArchiveEntry> entries = new(StringComparer.OrdinalIgnoreCase);
        private readonly FileStream fileStream;
        private readonly MemoryMappedFile mappedFile;

        public string Name { get; private set; }

        public int Count => entries.Count;

        public IEnumerable<FileArchiveEntry> Entries => from e in entries.Values select e;

        public FileArchive(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException("File archive was not found", filename);

            fileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            mappedFile = MemoryMappedFile.CreateFromFile(fileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            ReadTableOfContents();

            Name = filename;
        }

        public override string ToString() => $"{Name}, Entries = {entries.Count}";

        void ReadTableOfContents()
        {
            using var stream = mappedFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var reader = new BinaryReader(stream, Encoding.ASCII);

            var entryCount = reader.ReadUInt32() - 1;

            for (int i = 0; i < entryCount; i++)
            {
                var index = i;
                var startAddress = reader.ReadUInt32();
                var name = reader.ReadFixedString(NameLength).Trim();
                var size = reader.ReadInt32() - startAddress;

                reader.BaseStream.Position -= sizeof(uint);

                var entry = new FileArchiveEntry
                {
                    Index = index,
                    Name = name,
                    Offset = startAddress,
                    Size = size
                };

                entries[name] = entry;
            }
        }

        public bool ContainsFile(string filename)
        {
            CheckIfDisposed();
            return entries.ContainsKey(filename);
        }

        public FileArchiveEntry GetEntry(string filename)
        {
            CheckIfDisposed();
            return entries[filename];
        }

        public Stream GetStream(string filename)
        {
            CheckIfDisposed();

            var entry = entries[filename];
            var stream = mappedFile.CreateViewStream(entry.Offset, entry.Size, MemoryMappedFileAccess.Read);

            return stream;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                mappedFile?.Dispose();
                fileStream?.Dispose();
            }

            isDisposed = true;
        }

        private void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
