using System;
using System.Collections.Generic;
using System.Text;

namespace Lis.Core.Lis
{
    public sealed class LisDfsrParser
    {
        private const int EntryHeaderSize = 3;
        private const int SpecBlockSize = 40;

        private static readonly Encoding Encoding = Encoding.ASCII;

        public LisDataFormatSpecificationRecord Parse(LisLogicalRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if ((LisRecordType)record.Header.Type != LisRecordType.DataFormatSpecification)
            {
                throw new LisParseException("Invalid LIS record type for DFSR parser.");
            }

            byte[] data = record.Data;
            var entries = new List<LisDfsrEntryBlock>();
            int offset = 0;
            bool hasTerminator = false;

            while (offset < data.Length)
            {
                if (data.Length - offset < EntryHeaderSize)
                {
                    throw new LisParseException("Invalid DFSR: truncated entry block header.");
                }

                byte type = data[offset];
                byte size = data[offset + 1];
                byte reprc = data[offset + 2];
                offset += EntryHeaderSize;

                if (data.Length - offset < size)
                {
                    throw new LisParseException("Invalid DFSR: truncated entry block value.");
                }

                byte[] valueBytes = new byte[size];
                if (size > 0)
                {
                    Buffer.BlockCopy(data, offset, valueBytes, 0, size);
                    offset += size;
                }

                ParseEntryValue(reprc, valueBytes, out int? numericValue, out string? textValue);
                entries.Add(new LisDfsrEntryBlock(type, size, reprc, valueBytes, numericValue, textValue));

                if (type == (byte)LisDfsrEntryType.Terminator)
                {
                    hasTerminator = true;
                    break;
                }
            }

            if (!hasTerminator)
            {
                throw new LisParseException("Invalid DFSR: missing entry block terminator.");
            }

            byte subtype = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                LisDfsrEntryBlock entry = entries[i];
                if (entry.Type == (byte)LisDfsrEntryType.SpecBlockSubtype && entry.NumericValue.HasValue)
                {
                    subtype = entry.NumericValue.Value == 1 ? (byte)1 : (byte)0;
                }
            }

            int remaining = data.Length - offset;
            if (remaining % SpecBlockSize != 0)
            {
                throw new LisParseException(
                    "Invalid DFSR: trailing bytes after entries are not aligned to 40-byte spec blocks.");
            }

            int blockCount = remaining / SpecBlockSize;
            var specBlocks = new List<LisDfsrSpecBlock>(blockCount);
            for (int i = 0; i < blockCount; i++)
            {
                LisDfsrSpecBlock block = ParseSpecBlock(data, offset, subtype);
                specBlocks.Add(block);
                offset += SpecBlockSize;
            }

            return new LisDataFormatSpecificationRecord(entries, specBlocks, subtype);
        }

        private static LisDfsrSpecBlock ParseSpecBlock(byte[] data, int offset, byte subtype)
        {
            string mnemonic = DecodeString(data, offset + 0, 4);
            string serviceId = DecodeString(data, offset + 4, 6);
            string serviceOrderNumber = DecodeString(data, offset + 10, 8);
            string units = DecodeString(data, offset + 18, 4);
            short fileNumber = ReadInt16BigEndian(data, offset + 26);
            short reservedSize = ReadInt16BigEndian(data, offset + 28);
            byte samples = data[offset + 33];
            byte reprc = data[offset + 34];

            if (subtype == 0)
            {
                byte apiLogType = data[offset + 22];
                byte apiCurveType = data[offset + 23];
                byte apiCurveClass = data[offset + 24];
                byte apiModifier = data[offset + 25];
                byte processLevel = data[offset + 32];

                return new LisDfsrSpecBlock(
                    subtype,
                    mnemonic,
                    serviceId,
                    serviceOrderNumber,
                    units,
                    fileNumber,
                    reservedSize,
                    samples,
                    reprc,
                    apiLogType,
                    apiCurveType,
                    apiCurveClass,
                    apiModifier,
                    processLevel,
                    0,
                    Array.Empty<byte>());
            }

            int apiCodes = ReadInt32BigEndian(data, offset + 22);
            byte[] processIndicators = new byte[5];
            Buffer.BlockCopy(data, offset + 35, processIndicators, 0, 5);

            return new LisDfsrSpecBlock(
                subtype,
                mnemonic,
                serviceId,
                serviceOrderNumber,
                units,
                fileNumber,
                reservedSize,
                samples,
                reprc,
                0,
                0,
                0,
                0,
                0,
                apiCodes,
                processIndicators);
        }

        private static void ParseEntryValue(byte reprc, byte[] valueBytes, out int? numeric, out string? text)
        {
            numeric = null;
            text = null;

            if (valueBytes.Length == 0)
            {
                return;
            }

            switch ((LisRepresentationCode)reprc)
            {
                case LisRepresentationCode.Byte:
                    numeric = valueBytes[0];
                    return;

                case LisRepresentationCode.Int8:
                    numeric = unchecked((sbyte)valueBytes[0]);
                    return;

                case LisRepresentationCode.Int16:
                    if (valueBytes.Length >= 2)
                    {
                        numeric = ReadInt16BigEndian(valueBytes, 0);
                    }

                    return;

                case LisRepresentationCode.Int32:
                    if (valueBytes.Length >= 4)
                    {
                        numeric = ReadInt32BigEndian(valueBytes, 0);
                    }

                    return;

                case LisRepresentationCode.String:
                case LisRepresentationCode.Mask:
                    text = DecodeString(valueBytes, 0, valueBytes.Length);
                    return;

                default:
                    return;
            }
        }

        private static short ReadInt16BigEndian(byte[] data, int offset)
        {
            return (short)((data[offset] << 8) | data[offset + 1]);
        }

        private static int ReadInt32BigEndian(byte[] data, int offset)
        {
            return
                (data[offset] << 24) |
                (data[offset + 1] << 16) |
                (data[offset + 2] << 8) |
                data[offset + 3];
        }

        private static string DecodeString(byte[] data, int offset, int count)
        {
            return Encoding.GetString(data, offset, count).TrimEnd(' ', '\0');
        }
    }
}
