using System;
using System.Collections.Generic;
using System.IO;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Считывает LIS-поток в необработанное представление <see cref="LisDocument"/> в памяти.
    /// </summary>
    public sealed class LisImporter
    {
        /// <summary>
        /// Открывает и импортирует LIS-файл по указанному пути.
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
        /// Импортирует все логические записи из читаемого и позиционируемого потока.
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

            // Сохраняем исходную позицию: импорт не должен менять состояние вызывающего кода.
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
