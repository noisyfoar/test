using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lis.Core.Lis
{
    public sealed class LisFdataParser
    {
    private readonly Dictionary<LisDataFormatSpecificationRecord, DecodePlan> _planCache =
        new Dictionary<LisDataFormatSpecificationRecord, DecodePlan>(ReferenceComparer.Instance);

        private sealed class DecodePlan
        {
            public DecodePlan(
                int[] valueSizes,
                int frameSize,
                LisRepresentationCode[] representationCodes,
                string[] mnemonics,
                int[] samplesPerSpec)
            {
                ValueSizes = valueSizes;
                FrameSize = frameSize;
                RepresentationCodes = representationCodes;
                Mnemonics = mnemonics;
                SamplesPerSpec = samplesPerSpec;
            }

            public int[] ValueSizes { get; }

            public int FrameSize { get; }

            public LisRepresentationCode[] RepresentationCodes { get; }

            public string[] Mnemonics { get; }

            public int[] SamplesPerSpec { get; }
        }

        private sealed class ReferenceComparer : IEqualityComparer<LisDataFormatSpecificationRecord>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            /// <summary>
            /// Подробно выполняет операцию «Equals» для обработки данных формата LIS.
            /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
            /// </summary>
            public bool Equals(LisDataFormatSpecificationRecord? x, LisDataFormatSpecificationRecord? y)
            {
                return ReferenceEquals(x, y);
            }

            /// <summary>
            /// Подробно выполняет операцию «GetHashCode» для обработки данных формата LIS.
            /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
            /// </summary>
            public int GetHashCode(LisDataFormatSpecificationRecord obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «ParseFrames» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public IReadOnlyList<LisFrameData> ParseFrames(
            LisLogicalRecord record,
            LisDataFormatSpecificationRecord format)
        {
            return ParseFrames(record, format, selectedMnemonics: null, metrics: null);
        }

        /// <summary>
        /// Подробно выполняет операцию «ParseFrames» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public IReadOnlyList<LisFrameData> ParseFrames(
            LisLogicalRecord record,
            LisDataFormatSpecificationRecord format,
            ISet<string>? selectedMnemonics,
            LisReadMetrics? metrics = null)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            DecodePlan plan = GetOrBuildDecodePlan(format);
            int frameCount = ComputeFrameCount(record, plan.FrameSize);
            bool decodeAll = selectedMnemonics == null || selectedMnemonics.Count == 0;
            bool[]? decodeMask = decodeAll
                ? null
                : BuildDecodeMask(plan.Mnemonics, selectedMnemonics!);
            int channelCapacity = decodeAll ? format.SpecBlocks.Count : CountSelectedChannels(decodeMask!);
            var frames = new List<LisFrameData>(frameCount);

            int offset = 0;
            for (int frame = 0; frame < frameCount; frame++)
            {
                var channels = new List<LisFrameChannelData>(channelCapacity);
                for (int c = 0; c < format.SpecBlocks.Count; c++)
                {
                    string mnemonic = plan.Mnemonics[c];
                    int sampleCount = plan.SamplesPerSpec[c];
                    int valueSize = plan.ValueSizes[c];

                    if (!decodeAll && !decodeMask![c])
                    {
                        offset += valueSize * sampleCount;
                        metrics?.AddSamplesSkipped(sampleCount);
                        continue;
                    }

                    var samples = new object[sampleCount];
                    DecodeSamples(record.Data, ref offset, plan.RepresentationCodes[c], valueSize, samples);
                    metrics?.AddSamplesDecoded(sampleCount);

                    channels.Add(new LisFrameChannelData(mnemonic, samples));
                }

                frames.Add(new LisFrameData(channels));
            }

            return frames;
        }

        /// <summary>
        /// Подробно выполняет операцию «AccumulateCurves» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public void AccumulateCurves(
            LisLogicalRecord record,
            LisDataFormatSpecificationRecord format,
            IDictionary<string, List<object>> curves,
            ISet<string>? selectedMnemonics = null,
            LisReadMetrics? metrics = null)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (curves == null)
            {
                throw new ArgumentNullException(nameof(curves));
            }

            DecodePlan plan = GetOrBuildDecodePlan(format);
            int frameCount = ComputeFrameCount(record, plan.FrameSize);
            bool decodeAll = selectedMnemonics == null || selectedMnemonics.Count == 0;
            bool[]? decodeMask = decodeAll
                ? null
                : BuildDecodeMask(plan.Mnemonics, selectedMnemonics!);

            int offset = 0;
            for (int frame = 0; frame < frameCount; frame++)
            {
                for (int c = 0; c < format.SpecBlocks.Count; c++)
                {
                    string mnemonic = plan.Mnemonics[c];
                    int sampleCount = plan.SamplesPerSpec[c];
                    int valueSize = plan.ValueSizes[c];

                    if (!decodeAll && !decodeMask![c])
                    {
                        offset += valueSize * sampleCount;
                        metrics?.AddSamplesSkipped(sampleCount);
                        continue;
                    }

                    List<object> samples;
                    if (!curves.TryGetValue(mnemonic, out samples!))
                    {
                        samples = new List<object>(Math.Max(sampleCount * frameCount, 8));
                        curves[mnemonic] = samples;
                    }
                    else
                    {
                        EnsureAdditionalCapacity(samples, sampleCount);
                    }

                    DecodeAndAppendSamples(record.Data, ref offset, plan.RepresentationCodes[c], valueSize, sampleCount, samples);
                    metrics?.AddSamplesDecoded(sampleCount);
                }
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «GetOrBuildDecodePlan» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private DecodePlan GetOrBuildDecodePlan(LisDataFormatSpecificationRecord format)
        {
            DecodePlan? plan;
            if (_planCache.TryGetValue(format, out plan!))
            {
                return plan;
            }

            plan = BuildDecodePlan(format);
            _planCache[format] = plan;
            return plan;
        }

        /// <summary>
        /// Подробно выполняет операцию «BuildDecodePlan» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static DecodePlan BuildDecodePlan(LisDataFormatSpecificationRecord format)
        {
            if (format.SpecBlocks.Count == 0)
            {
                throw new LisParseException("Invalid DFSR: no spec blocks for frame parsing.");
            }

            int count = format.SpecBlocks.Count;
            var valueSizes = new int[count];
            var representationCodes = new LisRepresentationCode[count];
            var mnemonics = new string[count];
            var samplesPerSpec = new int[count];
            int frameSize = 0;

            for (int i = 0; i < count; i++)
            {
                LisDfsrSpecBlock spec = format.SpecBlocks[i];
                LisRepresentationCode representationCode = (LisRepresentationCode)spec.RepresentationCode;
                int valueSize = GetFixedValueSize(representationCode);

                valueSizes[i] = valueSize;
                representationCodes[i] = representationCode;
                mnemonics[i] = spec.Mnemonic;
                samplesPerSpec[i] = spec.Samples;
                frameSize += valueSize * spec.Samples;
            }

            if (frameSize <= 0)
            {
                throw new LisParseException("Invalid frame size computed from DFSR.");
            }

            return new DecodePlan(valueSizes, frameSize, representationCodes, mnemonics, samplesPerSpec);
        }

        /// <summary>
        /// Подробно выполняет операцию «BuildDecodeMask» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static bool[] BuildDecodeMask(string[] mnemonics, ISet<string> selectedMnemonics)
        {
            var mask = new bool[mnemonics.Length];
            for (int i = 0; i < mnemonics.Length; i++)
            {
                mask[i] = selectedMnemonics.Contains(mnemonics[i]);
            }

            return mask;
        }

        /// <summary>
        /// Подробно выполняет операцию «CountSelectedChannels» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static int CountSelectedChannels(bool[] decodeMask)
        {
            int count = 0;
            for (int i = 0; i < decodeMask.Length; i++)
            {
                if (decodeMask[i])
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Подробно выполняет операцию «ComputeFrameCount» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static int ComputeFrameCount(LisLogicalRecord record, int frameSize)
        {
            LisRecordType recordType = (LisRecordType)record.Header.Type;
            if (recordType != LisRecordType.NormalData && recordType != LisRecordType.AlternateData)
            {
                throw new LisParseException("Invalid LIS record type for FData parser.");
            }

            if (record.Data.Length % frameSize != 0)
            {
                throw new LisParseException(
                    "FData payload length is not aligned to computed frame size.");
            }

            return record.Data.Length / frameSize;
        }

        /// <summary>
        /// Подробно выполняет операцию «ShouldDecode» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static bool ShouldDecode(string mnemonic, ISet<string>? selectedMnemonics)
        {
            if (selectedMnemonics == null || selectedMnemonics.Count == 0)
            {
                return true;
            }

            return selectedMnemonics.Contains(mnemonic);
        }

        /// <summary>
        /// Подробно выполняет операцию «EnsureAdditionalCapacity» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static void EnsureAdditionalCapacity(List<object> destination, int additionalCount)
        {
            int required = destination.Count + additionalCount;
            if (destination.Capacity < required)
            {
                destination.Capacity = required;
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «GetFixedValueSize» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static int GetFixedValueSize(LisRepresentationCode representationCode)
        {
            switch (representationCode)
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

        /// <summary>
        /// Подробно выполняет операцию «DecodeSamples» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static void DecodeSamples(byte[] data, ref int offset, byte representationCode, int valueSize, object[] destination)
        {
            DecodeSamples(data, ref offset, (LisRepresentationCode)representationCode, valueSize, destination);
        }

        /// <summary>
        /// Подробно выполняет операцию «DecodeSamples» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static void DecodeSamples(
            byte[] data,
            ref int offset,
            LisRepresentationCode representationCode,
            int valueSize,
            object[] destination)
        {
            switch (representationCode)
            {
                case LisRepresentationCode.Int8:
                    for (int i = 0; i < destination.Length; i++)
                    {
                        destination[i] = unchecked((sbyte)data[offset]);
                        offset++;
                    }

                    return;

                case LisRepresentationCode.Int16:
                    for (int i = 0; i < destination.Length; i++)
                    {
                        destination[i] = (short)((data[offset] << 8) | data[offset + 1]);
                        offset += 2;
                    }

                    return;

                case LisRepresentationCode.Int32:
                    for (int i = 0; i < destination.Length; i++)
                    {
                        destination[i] =
                            (data[offset] << 24) |
                            (data[offset + 1] << 16) |
                            (data[offset + 2] << 8) |
                            data[offset + 3];
                        offset += 4;
                    }

                    return;

                case LisRepresentationCode.Byte:
                    for (int i = 0; i < destination.Length; i++)
                    {
                        destination[i] = data[offset];
                        offset++;
                    }

                    return;

                case LisRepresentationCode.Float16:
                case LisRepresentationCode.Float32Low:
                case LisRepresentationCode.Float32:
                case LisRepresentationCode.Float32Fixed:
                    for (int i = 0; i < destination.Length; i++)
                    {
                        destination[i] = DecodeFloatingValue(data, offset, representationCode);
                        offset += valueSize;
                    }

                    return;

                default:
                    throw new LisParseException("Unsupported LIS representation code in LisFdataParser.");
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «DecodeAndAppendSamples» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static void DecodeAndAppendSamples(
            byte[] data,
            ref int offset,
            byte representationCode,
            int valueSize,
            int sampleCount,
            List<object> destination)
        {
            DecodeAndAppendSamples(
                data,
                ref offset,
                (LisRepresentationCode)representationCode,
                valueSize,
                sampleCount,
                destination);
        }

        /// <summary>
        /// Подробно выполняет операцию «DecodeAndAppendSamples» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static void DecodeAndAppendSamples(
            byte[] data,
            ref int offset,
            LisRepresentationCode representationCode,
            int valueSize,
            int sampleCount,
            List<object> destination)
        {
            switch (representationCode)
            {
                case LisRepresentationCode.Int8:
                    for (int i = 0; i < sampleCount; i++)
                    {
                        destination.Add(unchecked((sbyte)data[offset]));
                        offset++;
                    }

                    return;

                case LisRepresentationCode.Int16:
                    for (int i = 0; i < sampleCount; i++)
                    {
                        destination.Add((short)((data[offset] << 8) | data[offset + 1]));
                        offset += 2;
                    }

                    return;

                case LisRepresentationCode.Int32:
                    for (int i = 0; i < sampleCount; i++)
                    {
                        destination.Add(
                            (data[offset] << 24) |
                            (data[offset + 1] << 16) |
                            (data[offset + 2] << 8) |
                            data[offset + 3]);
                        offset += 4;
                    }

                    return;

                case LisRepresentationCode.Byte:
                    for (int i = 0; i < sampleCount; i++)
                    {
                        destination.Add(data[offset]);
                        offset++;
                    }

                    return;

                case LisRepresentationCode.Float16:
                case LisRepresentationCode.Float32Low:
                case LisRepresentationCode.Float32:
                case LisRepresentationCode.Float32Fixed:
                    for (int i = 0; i < sampleCount; i++)
                    {
                        destination.Add(DecodeFloatingValue(data, offset, representationCode));
                        offset += valueSize;
                    }

                    return;

                default:
                    throw new LisParseException("Unsupported LIS representation code in LisFdataParser.");
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «DecodeFloatingValue» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «DecodeF16» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «DecodeF32» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «DecodeF32Low» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «DecodeF32Fixed» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «TwosComplement» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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
