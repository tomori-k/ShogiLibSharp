﻿using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public static UsiInfo ParseInfo(string command)
            => TryParseInfo(command, out var v) ? v : throw new FormatException("info コマンドの形式が正しくありません。");

        public static bool TryParseInfo(string command, out UsiInfo result)
        {
            result = new UsiInfo();
            if (!command.StartsWith("info")) return false;

            var sp = command.Split();

            foreach (var (name, value) in sp.Zip(sp.Skip(1)))
            {
                switch (name)
                {
                    case "depth":
                        { if (int.TryParse(value, out var v)) result.Depth = v; }
                        break;
                    case "seldepth":
                        { if (int.TryParse(value, out var v)) result.SelDepth = v; }
                        break;
                    case "time":
                        { if (int.TryParse(value, out var v)) result.Time = TimeSpan.FromMilliseconds(v); }
                        break;
                    case "nodes":
                        { if (ulong.TryParse(value, out var v)) result.Nodes = v; }
                        break;
                    case "multipv":
                        { if (int.TryParse(value, out var v)) result.MultiPv = v; }
                        break;
                    case "currmove":
                        { if (Usi.TryParseMove(value, out var v)) result.CurrMove = v; }
                        break;
                    case "hashfull":
                        { if (int.TryParse(value, out var v)) result.Hashfull = v; }
                        break;
                    case "nps":
                        { if (ulong.TryParse(value, out var v)) result.Nps = v; }
                        break;
                }
            }

            var scoreIdx = Array.IndexOf(sp, "score");
            if (scoreIdx != -1
                && scoreIdx + 2 < sp.Length
                && int.TryParse(sp[scoreIdx + 2], out var score))
            {
                var isMate = sp[scoreIdx + 1] == "mate";
                var bound = scoreIdx + 3 >= sp.Length ? Bound.Exact
                    : sp[scoreIdx + 3] == "upperbound" ? Bound.UpperBound
                    : sp[scoreIdx + 3] == "lowerbound" ? Bound.LowerBound
                    : Bound.Exact;
                result.Score = new Score(score, isMate, bound);
            }
            else if (scoreIdx != -1
                && scoreIdx + 2 < sp.Length
                && sp[scoreIdx + 1] == "mate"
                && (sp[scoreIdx + 2] == "+" || sp[scoreIdx + 2] == "-"))
            {
                var mateScore = sp[scoreIdx + 2] == "+" ? 1 : -1;
                var bound = mateScore > 0 ? Bound.LowerBound : Bound.UpperBound;
                result.Score = new Score(mateScore, true, bound);
            }

            var pvIdx = Array.IndexOf(sp, "pv");
            if (pvIdx != -1)
            {
                for (int i = pvIdx + 1; i < sp.Length; ++i)
                {
                    if (!Usi.TryParseMove(sp[i], out var m)) break;
                    result.Pv.Add(m);
                }
            }

            var strIdx = command.IndexOf("string ");
            if (strIdx != -1) result.String = command[(strIdx+7)..];

            return true;
        }

        static FileName ParseFileNameOption(string command, string[] sp)
        {
            if(sp.Length < 7)
            {
                throw new FormatException($"USI 形式の option コマンドではありません。：{command}");
            }
            return FileName.Create(sp[6] == "<empty>" ? "" : sp[6]);
        }

        static Options.String ParseStringOption(string command, string[] sp)
        {
            if (sp.Length < 7)
            {
                throw new FormatException($"USI 形式の option コマンドではありません。：{command}");
            }
            return Options.String.Create(sp[6] == "<empty>" ? "" : sp[6]);
        }

        static Combo ParseComboOption(string command, string[] sp)
        {
            var defaultValue = sp.SkipWhile(x => x != "default").Skip(1).FirstOrDefault();
            var items = sp.Zip(sp.Skip(1))
                .Where(x => x.First == "var")
                .Select(x => x.Second)
                .ToList();
            if (defaultValue is not null && items.Contains(defaultValue))
            {
                return Combo.Create(defaultValue, items);
            }
            else
            {
                throw new FormatException($"USI 形式の option コマンドではありません。：{command}");
            }
        }

        static IUsiOptionValue ParseCheckOption(string command, string[] sp)
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

        static Spin ParseSpinOption(string command, string[] sp)
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
