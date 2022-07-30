using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.Process;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiLibSharp.Engine.Tests
{
    internal class RandomPlayer : IEngineProcess
    {
        private Core.Position? pos = null;
        private Random rnd = new(0);
        private TaskCompletionSource? tcsGo = null;
        private TaskCompletionSource? tcsPonder = null;

        public event Action<string?>? StdOutReceived;
        public event Action<string?>? StdErrReceived;
        public event EventHandler Exited;

        public void BeginErrorReadLine() {}
        public void BeginOutputReadLine() {}
        public void Dispose() {}

        public void Kill()
        {
            Exited?.Invoke(this, EventArgs.Empty);
        }

        public void SendLine(string message)
        {
            Trace.WriteLine($"< {message}");
            if (message == "usi") Usi();
            else if (message == "isready") IsReady();
            else if (message.StartsWith("position")) Position(message);
            else if (message.StartsWith("go ponder")) GoPonder(message);
            else if (message.StartsWith("go")) Go(message);
            else if (message == "quit") Quit();
            else if (message == "stop") Stop();
            else if (message == "ponderhit") PonderHit();
        }

        private void PonderHit()
        {
            tcsPonder?.SetResult();
        }

        private void SendRandomMove()
        {
            var moves = Movegen.GenerateMoves(pos);
            var select = moves[rnd.Next(moves.Count)];
            pos.DoMove(select);
            if (pos.IsMated())
            {
                StdOutReceived?.Invoke($"bestmove {select.Usi()}");
            }
            else
            {
                var moves2 = Movegen.GenerateMoves(pos);
                var ponder = moves2[rnd.Next(moves2.Count)];
                StdOutReceived?.Invoke($"bestmove {select.Usi()} ponder {ponder.Usi()}");
            }
        }

        private async void GoPonder(string message)
        {
            tcsPonder = new TaskCompletionSource();
            await tcsPonder.Task;
            SendRandomMove();
        }

        private void Stop()
        {
            tcsGo?.SetResult();
            tcsPonder?.SetResult();
        }

        private void Quit()
        {
            Exited?.Invoke(this, EventArgs.Empty);
        }

        private async void Go(string message)
        {
            tcsGo = new TaskCompletionSource();
            var byoyomi = int.Parse(message.Split()[6]);

            await Task.WhenAny(Task.Delay(byoyomi), tcsGo.Task);

            SendRandomMove();
        }

        private void Position(string command)
        {
            var sfen = string.Join(' ', command.Split().Skip(2).Take(4));
            var moves = command
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .SkipWhile(x => x != "moves")
                .Skip(1)
                .Select(x => Core.Usi.ParseMove(x));
            pos = new Core.Position(sfen);
            foreach (var m in moves)
            {
                pos.DoMove(m);
            }
        }

        private async void IsReady()
        {
            await Task.Delay(10);
            StdOutReceived?.Invoke("readyok");
        }

        private void Usi()
        {
            StdOutReceived?.Invoke("id name RandomPlayer");
            StdOutReceived?.Invoke("id author Author0112");
            StdOutReceived?.Invoke("option name USI_Hash type spin default 1024 min 1 max 33554432");
            StdOutReceived?.Invoke("option name USI_Ponder type check default false");
            StdOutReceived?.Invoke("option name NodesLimit type spin default 0 min 0 max 9223372036854775807");
            StdOutReceived?.Invoke("option name BookEvalLimit type spin default 0 min -99999 max 99999");
            StdOutReceived?.Invoke("option name SomeFile type filename default <empty>");
            StdOutReceived?.Invoke("option name BookFile type combo default no_book var no_book var standard_book.db var yaneura_book1.db var yaneura_book2.db var yaneura_book3.db var yaneura_book4.db var user_book1.db var user_book2.db var user_book3.db var book.bin");
            StdOutReceived?.Invoke("usiok");
        }

        public bool Start()
        {
            return true;
        }
    }
}
