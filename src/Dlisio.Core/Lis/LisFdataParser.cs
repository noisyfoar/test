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
                    var raw = new byte[valueSize];
                    Buffer.BlockCopy(data, offset, raw, 0, valueSize);
                    return raw;

                default:
                    throw new LisParseException("Unsupported LIS representation code in LisFdataParser.");
            }
        }
    }
}
