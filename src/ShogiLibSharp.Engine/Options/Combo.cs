using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.Options
{
    public class Combo : IUsiOptionValue
    {
        public string Default { get; set; } = "";
        public List<string> Items { get; set; } = new();
        public string Value { get; set; } = "";

        public static Combo Create(string defaultValue, List<string> items)
        {
            return new Combo
            {
                Default = defaultValue,
                Items = items.ToList(),
                Value = defaultValue,
            };
        }
    }
}
