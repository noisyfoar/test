using System;
using System.Collections.Generic;

namespace Lis.Core.Lis
{
    public sealed class LisReadOptions
    {
        public LisReadOptions(
            IReadOnlyCollection<string>? selectedCurveMnemonics = null,
            bool includeFrames = true,
            bool includeCurves = false)
        {
            SelectedCurveMnemonics = selectedCurveMnemonics;
            IncludeFrames = includeFrames;
            IncludeCurves = includeCurves;
        }

        public IReadOnlyCollection<string>? SelectedCurveMnemonics { get; }

        public bool IncludeFrames { get; }

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
