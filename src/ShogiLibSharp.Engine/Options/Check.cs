using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.Options
{
    public class Check : IUsiOptionValue
    {
        public bool Default { get; set; }
        public bool Value { get; set; }

        string IUsiOptionValue.Value => Value.ToString().ToLower();

        public static Check Create(bool defaultValue)
        {
            return new Check
            {
                Default = defaultValue,
                Value = defaultValue,
            };
        }
    }
}
