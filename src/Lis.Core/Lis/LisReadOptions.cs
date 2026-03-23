using System;
using System.Collections.Generic;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Настраивает, как <see cref="LisFileParser"/> материализует разобранные данные.
    /// </summary>
    public sealed class LisReadOptions
    {
        /// <summary>
        /// Создаёт набор опций для выборочной материализации каналов и кадров.
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
        /// Опциональный фильтр по мнемоникам; если пусто, разбираются все каналы.
        /// </summary>
        public IReadOnlyCollection<string>? SelectedCurveMnemonics { get; }

        /// <summary>
        /// Если значение включено, в результат добавляются объекты кадров.
        /// </summary>
        public bool IncludeFrames { get; }

        /// <summary>
        /// Если значение включено, парсер накапливает сэмплы в словарь кривых.
        /// </summary>
        public bool IncludeCurves { get; }

        /// <summary>
        /// Подробно выполняет операцию «BuildSelectedCurveSet» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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
