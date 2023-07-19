using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.Options
{
    public class FileName : IUsiOptionValue
    {
        public string Default { get; set; } = "";
        public string Value { get; set; } = "";

        public static FileName Create(string defaultValue)
        {
            return new FileName
            {
                Default = defaultValue,
                Value = defaultValue,
            };
        }
    }
}
