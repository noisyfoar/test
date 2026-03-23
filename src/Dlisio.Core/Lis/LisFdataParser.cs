using System;
using System.Collections.Generic;

namespace Dlisio.Core.Lis
{
    public sealed class LisFdataParser
    {
        public IReadOnlyList<LisFrameData> ParseFrames(
            LisLogicalRecord record,
            LisDataFormatSpecificationRecord format)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            LisRecordType recordType = (LisRecordType)record.Header.Type;
            if (recordType != LisRecordType.NormalData && recordType != LisRecordType.AlternateData)
            {
                throw new LisParseException("Invalid LIS record type for FData parser.");
            }

            if (format.SpecBlocks.Count == 0)
            {
                throw new LisParseException("Invalid DFSR: no spec blocks for frame parsing.");
            }

            int frameSize = 0;
            for (int i = 0; i < format.SpecBlocks.Count; i++)
            {
                LisDfsrSpecBlock spec = format.SpecBlocks[i];
                int valueSize = GetFixedValueSize(spec.RepresentationCode);
                frameSize += valueSize * spec.Samples;
            }

            if (frameSize <= 0)
            {
                throw new LisParseException("Invalid frame size computed from DFSR.");
            }

            if (record.Data.Length % frameSize != 0)
            {
                throw new LisParseException(
                    "FData payload length is not aligned to computed frame size.");
            }

            int frameCount = record.Data.Length / frameSize;
            var frames = new List<LisFrameData>(frameCount);

            int offset = 0;
            for (int frame = 0; frame < frameCount; frame++)
            {
                var channels = new List<LisFrameChannelData>(format.SpecBlocks.Count);
                for (int c = 0; c < format.SpecBlocks.Count; c++)
                {
                    LisDfsrSpecBlock spec = format.SpecBlocks[c];
                    int sampleCount = spec.Samples;
                    int valueSize = GetFixedValueSize(spec.RepresentationCode);
                    var samples = new object[sampleCount];

                    for (int s = 0; s < sampleCount; s++)
                    {
                        samples[s] = DecodeValue(record.Data, offset, spec.RepresentationCode, valueSize);
                        offset += valueSize;
                    }

                    channels.Add(new LisFrameChannelData(spec.Mnemonic, samples));
                }

                frames.Add(new LisFrameData(channels));
            }

            return frames;
        }

        private static int GetFixedValueSize(byte representationCode)
        {
            switch ((LisRepresentationCode)representationCode)
            {
                case LisRepresentationCode.Int8:
                case LisRepresentationCode.Byte:
                    return 1;

                case LisRepresentationCode.Int16:
                case LisRepresentationCode.Float16:
                    return 2;

                case LisRepresentationCode.Int32:
                case LisRepresentationCode.Float32Low:
                case LisRepresentationCode.Float32:
                case LisRepresentationCode.Float32Fixed:
                    return 4;

                case LisRepresentationCode.String:
                case LisRepresentationCode.Mask:
                    throw new LisParseException(
                        "Variable-length representation codes are not supported in LisFdataParser.");

                default:
                    throw new LisParseException("Unsupported LIS representation code in LisFdataParser.");
            }
        }

        private static object DecodeValue(byte[] data, int offset, byte representationCode, int valueSize)
        {
            switch ((LisRepresentationCode)representationCode)
            {
                case LisRepresentationCode.Int8:
                    return unchecked((sbyte)data[offset]);

                case LisRepresentationCode.Int16:
                    return (short)((data[offset] << 8) | data[offset + 1]);

                case LisRepresentationCode.Int32:
                    return
                        (data[offset] << 24) |
                        (data[offset + 1] << 16) |
                        (data[offset + 2] << 8) |
                        data[offset + 3];

                case LisRepresentationCode.Byte:
                    return data[offset];

                case LisRepresentationCode.Float16:
                case LisRepresentationCode.Float32Low:
                case LisRepresentationCode.Float32:
                case LisRepresentationCode.Float32Fixed:
                    return DecodeFloatingValue(data, offset, (LisRepresentationCode)representationCode);

                default:
                    throw new LisParseException("Unsupported LIS representation code in LisFdataParser.");
            }
        }

        private static float DecodeFloatingValue(byte[] data, int offset, LisRepresentationCode code)
        {
            switch (code)
            {
                case LisRepresentationCode.Float16:
                    return DecodeF16(data, offset);

                case LisRepresentationCode.Float32:
                    return DecodeF32(data, offset);

                case LisRepresentationCode.Float32Low:
                    return DecodeF32Low(data, offset);

                case LisRepresentationCode.Float32Fixed:
                    return DecodeF32Fixed(data, offset);

                default:
                    throw new LisParseException("Unsupported floating representation code.");
            }
        }

        private static float DecodeF16(byte[] data, int offset)
        {
            ushort value = (ushort)((data[offset] << 8) | data[offset + 1]);

            int signBit = (value & 0x8000) >> 15;
            int expBits = (value & 0x000F);
            uint fracBits = (uint)((value & 0x7FF0) >> 4);

            float sign = signBit == 1 ? -1.0f : 1.0f;
            float exponent = expBits;
            uint frac2Complement = TwosComplement(signBit == 1, fracBits, 11);
            float fraction = (float)(frac2Complement * Math.Pow(2.0, -11));

            return (float)(sign * fraction * Math.Pow(2.0, exponent));
        }

        private static float DecodeF32(byte[] data, int offset)
        {
            uint value =
                ((uint)data[offset] << 24) |
                ((uint)data[offset + 1] << 16) |
                ((uint)data[offset + 2] << 8) |
                data[offset + 3];

            int signBit = (int)((value & 0x80000000) >> 31);
            byte expBits = (byte)((value & 0x7F800000) >> 23);
            uint fracBits = value & 0x007FFFFF;

            float sign = signBit == 1 ? -1.0f : 1.0f;
            byte exponentOnesComplement = signBit == 1 ? (byte)~expBits : expBits;
            float exponent = exponentOnesComplement - 128.0f;
            uint frac2Complement = TwosComplement(signBit == 1, fracBits, 23);
            float fraction = (float)(frac2Complement * Math.Pow(2.0, -23));

            return (float)(sign * fraction * Math.Pow(2.0, exponent));
        }

        private static float DecodeF32Low(byte[] data, int offset)
        {
            uint value =
                ((uint)data[offset] << 24) |
                ((uint)data[offset + 1] << 16) |
                ((uint)data[offset + 2] << 8) |
                data[offset + 3];

            int fractionSignBit = (int)((value & 0x00008000) >> 15);
            int exponentSignBit = (int)((value & 0x80000000) >> 31);
            uint expBits = (value & 0x7FFF0000) >> 16;
            uint fracBits = value & 0x00007FFF;

            float fractionSign = fractionSignBit == 1 ? -1.0f : 1.0f;
            float exponentSign = exponentSignBit == 1 ? -1.0f : 1.0f;

            uint exp2Complement = TwosComplement(exponentSignBit == 1, expBits, 15);
            float exponent = exponentSign * exp2Complement;
            uint frac2Complement = TwosComplement(fractionSignBit == 1, fracBits, 15);

            return (float)(fractionSign * frac2Complement * Math.Pow(2.0, exponent - 15.0f));
        }

        private static float DecodeF32Fixed(byte[] data, int offset)
        {
            uint value =
                ((uint)data[offset] << 24) |
                ((uint)data[offset + 1] << 16) |
                ((uint)data[offset + 2] << 8) |
                data[offset + 3];

            int signBit = (int)((value & 0x80000000) >> 31);
            uint dataBits = value & 0x7FFFFFFF;
            float sign = signBit == 1 ? -1.0f : 1.0f;

            uint data2Complement = TwosComplement(signBit == 1, dataBits, 31);
            uint integerBits = (data2Complement & 0xFFFF0000) >> 16;
            uint realBits = data2Complement & 0x0000FFFF;

            float integerPart = integerBits;
            float realPart = (float)(realBits * Math.Pow(2.0, -16));

            return sign * (integerPart + realPart);
        }

        private static uint TwosComplement(bool isNegative, uint value, byte bitLength)
        {
            if (!isNegative)
            {
                return value;
            }

            uint mask = (uint)((1UL << bitLength) - 1UL);
            uint onesComplement = (~value) & mask;
            return onesComplement + 1U;
        }
    }
}
