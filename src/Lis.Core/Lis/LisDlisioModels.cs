using System;
using System.Collections.Generic;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Результат чтения LIS через Python-библиотеку dlisio.
    /// </summary>
    public sealed class LisDlisioSummary
    {
        public LisDlisioSummary(IReadOnlyList<LisDlisioLogicalFileSummary> logicalFiles, IReadOnlyList<string> errors)
        {
            LogicalFiles = logicalFiles ?? throw new ArgumentNullException(nameof(logicalFiles));
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        public IReadOnlyList<LisDlisioLogicalFileSummary> LogicalFiles { get; }

        /// <summary>
        /// Нефатальные сообщения/ошибки, собранные bridge-скриптом.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }
    }

    public sealed class LisDlisioLogicalFileSummary
    {
        public LisDlisioLogicalFileSummary(
            int index,
            string? fileHeaderName,
            string? fileTrailerName,
            int textRecordCount,
            IReadOnlyList<LisDlisioDfsrSummary> dfsrs)
        {
            Index = index;
            FileHeaderName = fileHeaderName;
            FileTrailerName = fileTrailerName;
            TextRecordCount = textRecordCount;
            Dfsrs = dfsrs ?? throw new ArgumentNullException(nameof(dfsrs));
        }

        public int Index { get; }

        public string? FileHeaderName { get; }

        public string? FileTrailerName { get; }

        public int TextRecordCount { get; }

        public IReadOnlyList<LisDlisioDfsrSummary> Dfsrs { get; }
    }

    public sealed class LisDlisioDfsrSummary
    {
        public LisDlisioDfsrSummary(int index, int subtype, IReadOnlyList<int> sampleRates, IReadOnlyList<LisDlisioChannelSummary> channels)
        {
            Index = index;
            Subtype = subtype;
            SampleRates = sampleRates ?? throw new ArgumentNullException(nameof(sampleRates));
            Channels = channels ?? throw new ArgumentNullException(nameof(channels));
        }

        public int Index { get; }

        public int Subtype { get; }

        public IReadOnlyList<int> SampleRates { get; }

        public IReadOnlyList<LisDlisioChannelSummary> Channels { get; }
    }

    public sealed class LisDlisioChannelSummary
    {
        public LisDlisioChannelSummary(string mnemonic, string units, int samples, int representationCode)
        {
            if (mnemonic == null)
            {
                throw new ArgumentNullException(nameof(mnemonic));
            }

            if (units == null)
            {
                throw new ArgumentNullException(nameof(units));
            }

            Mnemonic = mnemonic;
            Units = units;
            Samples = samples;
            RepresentationCode = representationCode;
        }

        public string Mnemonic { get; }

        public string Units { get; }

        public int Samples { get; }

        public int RepresentationCode { get; }
    }
}
