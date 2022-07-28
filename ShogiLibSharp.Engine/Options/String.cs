using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.Options
{
    public class String : IUsiOptionValue
    {
        public string Default { get; set; } = "";
        public string Value { get; set; } = "";

        public static String Create(string defaultValue)
        {
            return new String
            {
                Default = defaultValue,
                Value = defaultValue,
            };
        }
    }
}
