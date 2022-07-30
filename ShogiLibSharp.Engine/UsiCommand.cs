using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine
{
    internal static class UsiCommand
    {
        public static (Move, Move) ParseBestmove(string command)
        {
            var sp = command.Split();
            if (sp.Length < 2)
            {
                throw new FormatException($"USI 形式の bestmove コマンドではありません。：{command}");
            }
            return sp.Length < 4
                ? (Usi.ParseMove(sp[1]), Move.None)
                : (Usi.ParseMove(sp[1]), Usi.ParseMove(sp[3]));
        }

        public static (string, IUsiOptionValue) ParseOption(string command)
        {
            var sp = command.Split();
            if (sp.Length < 5)
            {
                throw new FormatException($"USI 形式の option コマンドではありません。：{command}");
            }
            var name = sp[2];
            var typeName = sp[4];
            return typeName switch
            {
                "check" => (name, ParseCheckOption(command, sp)),
                "spin" => (name, ParseSpinOption(command, sp)),
                "combo" => (name, ParseComboOption(command, sp)),
                "string" => (name, ParseStringOption(command, sp)),
                "filename" => (name, ParseFileNameOption(command, sp)),
                _ => throw new NotImplementedException(),
            };
        }

        private static FileName ParseFileNameOption(string command, string[] sp)
        {
            if(sp.Length < 7)
            {
                throw new FormatException($"USI 形式の option コマンドではありません。：{command}");
            }
            return FileName.Create(sp[6] == "<empty>" ? "" : sp[6]);
        }

        private static Options.String ParseStringOption(string command, string[] sp)
        {
            if (sp.Length < 7)
            {
                throw new FormatException($"USI 形式の option コマンドではありません。：{command}");
            }
            return Options.String.Create(sp[6] == "<empty>" ? "" : sp[6]);
        }

        private static Combo ParseComboOption(string command, string[] sp)
        {
            var defaultValue = sp.SkipWhile(x => x != "default").Skip(1).FirstOrDefault();
            var items = sp.Zip(sp.Skip(1))
                .Where(x => x.First == "var")
                .Select(x => x.Second)
                .ToList();
            if (items.Contains(defaultValue))
            {
                return Combo.Create(defaultValue, items);
            }
            else
            {
                throw new FormatException($"USI 形式の option コマンドではありません。：{command}");
            }
        }

        private static IUsiOptionValue ParseCheckOption(string command, string[] sp)
        {
            if (sp.Length < 7)
            {
                throw new FormatException($"USI 形式の option コマンドではありません。：{command}");
            }
            if (bool.TryParse(sp[6], out var value))
            {
                return Check.Create(value);
            }
            else
            {
                throw new FormatException($"USI 形式の option コマンドではありません。bool 値に変換できません：{command}");
            }
        }

        private static Spin ParseSpinOption(string command, string[] sp)
        {
            if (sp.Length < 11)
            {
                throw new FormatException($"USI 形式の option コマンドではありません。：{command}");
            }

            string defaultStr, minStr, maxStr;
            try
            {
                defaultStr = sp.SkipWhile(x => x != "default").Skip(1).First();
                minStr = sp.SkipWhile(x => x != "min").Skip(1).First();
                maxStr = sp.SkipWhile(x => x != "max").Skip(1).First();
            }
            catch(InvalidOperationException e)
            {
                throw new FormatException($"USI 形式の option コマンドではありません。値が欠けています：{command}", e);
            }

            if (long.TryParse(defaultStr, out var defaultValue)
                && long.TryParse(minStr, out var min)
                && long.TryParse(maxStr, out var max))
            {
                return Spin.Create(defaultValue, min, max);
            }
            else
                throw new FormatException($"USI 形式の option コマンドではありません。数値に変換できません：{command}");
        }
    }
}
