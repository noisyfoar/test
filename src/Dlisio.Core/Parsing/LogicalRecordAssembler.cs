using System;
using System.Collections.Generic;

namespace Dlisio.Core.Parsing
{
    public static class LogicalRecordAssembler
    {
        public static LogicalRecord Assemble(IReadOnlyList<LogicalRecordSegment> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            if (segments.Count == 0)
            {
                throw new DlisParseException("Cannot assemble a logical record from zero segments.");
            }

            LogicalRecordSegment first = segments[0];
            if (!first.Header.IsFirstSegment)
            {
                throw new DlisParseException(
                    "Invalid logical record sequence: first segment is not marked as first.");
            }

            byte recordType = first.Header.LogicalRecordType;
            bool explicitlyFormatted = first.Header.IsExplicitlyFormatted;
            bool encrypted = first.Header.IsEncrypted;

            int totalBodyLength = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                LogicalRecordSegment segment = segments[i];
                LogicalRecordSegmentHeader header = segment.Header;

                if (header.LogicalRecordType != recordType)
                {
                    throw new DlisParseException(
                        "Invalid logical record sequence: logical record type changed between segments.");
                }

                if (header.IsExplicitlyFormatted != explicitlyFormatted)
                {
                    throw new DlisParseException(
                        "Invalid logical record sequence: structure bit changed between segments.");
                }

                if (header.IsEncrypted != encrypted)
                {
                    throw new DlisParseException(
                        "Invalid logical record sequence: encryption bit changed between segments.");
                }

                bool isFirst = i == 0;
                bool isLast = i == segments.Count - 1;

                if (!isFirst && header.IsFirstSegment)
                {
                    throw new DlisParseException(
                        "Invalid logical record sequence: non-first segment is marked as first.");
                }

                if (!isLast && header.IsLastSegment)
                {
                    throw new DlisParseException(
                        "Invalid logical record sequence: segment chain ended before the final segment.");
                }

                if (isLast && !header.IsLastSegment)
                {
                    throw new DlisParseException(
                        "Invalid logical record sequence: final segment is not marked as last.");
                }

                totalBodyLength += segment.Body.Length;
            }

            byte[] body = new byte[totalBodyLength];
            int offset = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                byte[] segmentBody = segments[i].Body;
                if (segmentBody.Length == 0)
                {
                    continue;
                }

                Buffer.BlockCopy(segmentBody, 0, body, offset, segmentBody.Length);
                offset += segmentBody.Length;
            }

            return new LogicalRecord(
                recordType,
                explicitlyFormatted,
                encrypted,
                segments,
                body);
        }
    }
}
