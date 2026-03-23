using System;
using System.Collections.Generic;
using System.IO;

namespace Dlisio.Core.Lis
{
    public sealed class LisReader
    {
        public LisLogicalRecord ReadNextLogicalRecord(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            LisPhysicalRecordHeader firstPrh = ReadNextPhysicalRecordHeader(stream);
            if (firstPrh.HasPredecessor)
            {
                throw new LisParseException(
                    "Invalid LIS layout: first physical record in a logical record is marked as continuation.");
            }

            var logicalRecordPayload = new List<byte>(Math.Max(0, firstPrh.Length - LisPhysicalRecordHeader.HeaderLength));
            LisLogicalRecordHeader? logicalRecordHeader = null;
            LisPhysicalRecordHeader currentHeader = firstPrh;
            int recordCount = 0;

            while (true)
            {
                recordCount++;

                int payloadLength = currentHeader.Length - LisPhysicalRecordHeader.HeaderLength - currentHeader.TrailerLength;
                if (payloadLength < 0)
                {
                    throw new LisParseException("Invalid LIS physical record length.");
                }

                byte[] payload = ReadExactly(stream, payloadLength, "LIS physical record payload");
                SkipBytes(stream, currentHeader.TrailerLength, "LIS physical record trailer");

                int payloadOffset = 0;
                if (!currentHeader.HasPredecessor)
                {
                    if (payload.Length < LisLogicalRecordHeader.HeaderLength)
                    {
                        throw new LisParseException(
                            "Invalid LIS physical record: first segment does not contain a full LRH.");
                    }

                    logicalRecordHeader = LisHeaderParser.ParseLogicalRecordHeader(payload, 0);
                    payloadOffset = LisLogicalRecordHeader.HeaderLength;
                }

                if (payload.Length > payloadOffset)
                {
                    int count = payload.Length - payloadOffset;
                    for (int i = 0; i < count; i++)
                    {
                        logicalRecordPayload.Add(payload[payloadOffset + i]);
                    }
                }

                if (!currentHeader.HasSuccessor)
                {
                    break;
                }

                currentHeader = ReadNextPhysicalRecordHeader(stream);
                if (!currentHeader.HasPredecessor)
                {
                    throw new LisParseException(
                        "Invalid LIS layout: successor chain broken (missing predecessor bit in continuation record).");
                }
            }

            if (logicalRecordHeader == null)
            {
                throw new LisParseException("Unable to read LIS logical record header.");
            }

            return new LisLogicalRecord(logicalRecordHeader, logicalRecordPayload.ToArray(), recordCount);
        }

        public LisPhysicalRecordHeader ReadNextPhysicalRecordHeader(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            byte[] headerBytes = ReadExactly(stream, LisPhysicalRecordHeader.HeaderLength, "LIS physical record header");

            // Skip obvious 4-byte pad blocks (0x00... or 0x20...).
            while (LisHeaderParser.IsPadBytes(headerBytes, 0, headerBytes.Length))
            {
                headerBytes = ReadExactly(
                    stream,
                    LisPhysicalRecordHeader.HeaderLength,
                    "LIS physical record header after padding");
            }

            return LisHeaderParser.ParsePhysicalRecordHeader(headerBytes, 0);
        }

        private static byte[] ReadExactly(Stream stream, int count, string componentName)
        {
            var buffer = new byte[count];
            int totalRead = 0;
            while (totalRead < count)
            {
                int n = stream.Read(buffer, totalRead, count - totalRead);
                if (n == 0)
                {
                    throw new LisParseException("Unexpected end of stream while reading " + componentName + ".");
                }

                totalRead += n;
            }

            return buffer;
        }

        private static void SkipBytes(Stream stream, int count, string componentName)
        {
            if (count <= 0)
            {
                return;
            }

            ReadExactly(stream, count, componentName);
        }
    }
}
