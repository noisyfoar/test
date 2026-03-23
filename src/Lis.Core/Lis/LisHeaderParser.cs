using System;

namespace Lis.Core.Lis
{
    public static class LisHeaderParser
    {
        public static LisPhysicalRecordHeader ParsePhysicalRecordHeader(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return ParsePhysicalRecordHeader(data, 0);
        }

        public static LisPhysicalRecordHeader ParsePhysicalRecordHeader(byte[] data, int offset)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (offset < 0 || offset > data.Length - LisPhysicalRecordHeader.HeaderLength)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            ushort length = ReadUInt16BigEndian(data, offset);
            ushort attributes = ReadUInt16BigEndian(data, offset + 2);
            var header = new LisPhysicalRecordHeader(length, attributes);

            if (header.Length < header.MinimumValidLength)
            {
                throw new LisParseException(
                    "Invalid LIS physical record length: shorter than the minimum required by its attributes.");
            }

            return header;
        }

        public static LisLogicalRecordHeader ParseLogicalRecordHeader(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return ParseLogicalRecordHeader(data, 0);
        }

        public static LisLogicalRecordHeader ParseLogicalRecordHeader(byte[] data, int offset)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (offset < 0 || offset > data.Length - LisLogicalRecordHeader.HeaderLength)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            byte type = data[offset];
            byte attributes = data[offset + 1];
            if (!LisRecordTypeHelper.IsValid(type))
            {
                throw new LisParseException("Invalid LIS logical record type.");
            }

            return new LisLogicalRecordHeader(type, attributes);
        }

        public static bool IsPadBytes(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (count <= 0)
            {
                return false;
            }

            if (offset < 0 || count < 0 || offset > bytes.Length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            byte first = bytes[offset];
            if (first != 0x00 && first != 0x20)
            {
                return false;
            }

            for (int i = 1; i < count; i++)
            {
                if (bytes[offset + i] != first)
                {
                    return false;
                }
            }

            return true;
        }

        private static ushort ReadUInt16BigEndian(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }
    }
}
