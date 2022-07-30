using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.Options
{
    public class Spin : IUsiOptionValue
    {
        public long Default { get; set; }
        public long Min { get; set; }
        public long Max { get; set; }
        public long Value { get; set; }

        string IUsiOptionValue.Value => Value.ToString();

        public static Spin Create(long defaultValue, long min, long max)
        {
            return new Spin
            {
                Default = defaultValue,
                Min = min,
                Max = max,
                Value = defaultValue,
            };
        }
    }
}
