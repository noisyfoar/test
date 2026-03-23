using System;
using System.Collections.Generic;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Configures how <see cref="LisFileParser"/> materializes parsed data.
    /// </summary>
    public sealed class LisReadOptions
    {
        /// <summary>
        /// Creates parser options for selective channel/frame materialization.
        /// </summary>
        public LisReadOptions(
            IReadOnlyCollection<string>? selectedCurveMnemonics = null,
            bool includeFrames = true,
            bool includeCurves = false)
        {
            SelectedCurveMnemonics = selectedCurveMnemonics;
            IncludeFrames = includeFrames;
            IncludeCurves = includeCurves;
        }

        /// <summary>
        /// Optional mnemonic filter. If null/empty all channels are parsed.
        /// </summary>
        public IReadOnlyCollection<string>? SelectedCurveMnemonics { get; }

        /// <summary>
        /// When true, parsed frame objects are included in the result.
        /// </summary>
        public bool IncludeFrames { get; }

        /// <summary>
        /// When true, parser accumulates samples into the Curves dictionary.
        /// </summary>
        public bool IncludeCurves { get; }

        internal HashSet<string>? BuildSelectedCurveSet()
        {
            if (SelectedCurveMnemonics == null || SelectedCurveMnemonics.Count == 0)
            {
                return null;
            }

            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string mnemonic in SelectedCurveMnemonics)
            {
                if (string.IsNullOrWhiteSpace(mnemonic))
                {
                    continue;
                }

                selected.Add(mnemonic.Trim());
            }

            return selected.Count == 0 ? null : selected;
        }
    }
}
