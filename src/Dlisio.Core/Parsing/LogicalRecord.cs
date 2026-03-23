using System.Collections.Generic;

namespace Dlisio.Core.Parsing
{
    public sealed class LogicalRecord
    {
        public LogicalRecord(
            byte logicalRecordType,
            bool isExplicitlyFormatted,
            bool isEncrypted,
            IReadOnlyList<LogicalRecordSegment> segments,
            byte[] body)
        {
            LogicalRecordType = logicalRecordType;
            IsExplicitlyFormatted = isExplicitlyFormatted;
            IsEncrypted = isEncrypted;
            Segments = segments;
            Body = body;
        }

        public byte LogicalRecordType { get; }

        public bool IsExplicitlyFormatted { get; }

        public bool IsEncrypted { get; }

        public IReadOnlyList<LogicalRecordSegment> Segments { get; }

        public byte[] Body { get; }
    }
}
