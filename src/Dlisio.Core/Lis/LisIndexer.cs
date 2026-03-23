using System;
using System.Collections.Generic;
using System.IO;

namespace Dlisio.Core.Lis
{
    public sealed class LisIndexer
    {
        public LisRecordIndex Index(Stream stream)
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

            var reader = new LisReader();
            var records = new List<LisRecordInfo>();

            while (true)
            {
                long offset = stream.Position;
                if (!reader.TryReadNextLogicalRecord(stream, out LisLogicalRecord? record))
                {
                    break;
                }
                if (record == null)
                {
                    throw new LisParseException("LIS reader returned no record after successful read.");
                }

                var info = new LisRecordInfo(
                    offset,
                    (LisRecordType)record.Header.Type,
                    record.Header.Attributes,
                    record.PhysicalRecordCount,
                    record.Data.Length);

                records.Add(info);
            }

            return new LisRecordIndex(records);
        }
    }
}
