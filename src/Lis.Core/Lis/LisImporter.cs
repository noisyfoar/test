using System;
using System.Collections.Generic;
using System.IO;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Reads a LIS stream into a raw in-memory <see cref="LisDocument"/>.
    /// </summary>
    public sealed class LisImporter
    {
        /// <summary>
        /// Opens and imports a LIS file by path.
        /// </summary>
        public LisDocument Import(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Trim().Length == 0)
            {
                throw new ArgumentException("Path must not be empty.", nameof(path));
            }

            using var stream = File.OpenRead(path);
            return Import(stream);
        }

        /// <summary>
        /// Imports all logical records from a readable, seekable stream.
        /// </summary>
        public LisDocument Import(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable.", nameof(stream));
            }

            // Preserve caller stream state; importer is non-destructive.
            long originalPosition = stream.Position;
            stream.Position = 0;
            try
            {
                var reader = new LisReader();
                var records = new List<LisLogicalRecord>();
                while (reader.TryReadNextLogicalRecord(stream, out LisLogicalRecord? record))
                {
                    if (record == null)
                    {
                        throw new LisParseException("LIS reader returned no logical record after successful read.");
                    }

                    records.Add(record);
                }

                return new LisDocument(records);
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }
    }
}
