using System.Collections.Generic;

namespace Lis.Core.Lis
{
    public sealed class LisFrameData
    {
        /// <summary>
        /// Подробно выполняет операцию «LisFrameData» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisFrameData(IReadOnlyList<LisFrameChannelData> channels)
        {
            Channels = channels;
        }

        public IReadOnlyList<LisFrameChannelData> Channels { get; }
    }
}
