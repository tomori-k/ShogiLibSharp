using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.State;
using System.Diagnostics;

namespace ShogiLibSharp.Engine
{
    public class UsiEngine
    {
        private Process? process;
        private object stateSyncObj = new();
        private StateBase currentState = new Deactivated();

        internal void SetStateWithLock(StateBase newState)
        {
            lock (stateSyncObj)
            {
                this.currentState = newState;
            }
        }

        public async Task BeginAsync(string fileName, string arguments)
        {
            if (currentState is Invalid)
            {
                throw new ObjectDisposedException(nameof(currentState));
            }

            var si = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            process = new Process()
            {
                StartInfo = si,
                EnableRaisingEvents = true,
            };
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.Exited += Process_Exited;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // send usi
            process.StandardInput.WriteLine("usi");

            SetStateWithLock(new AwaitingUsiOk());

            await ((AwaitingUsiOk)currentState).Tcs.Task;
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            currentState.Exited(this);
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == "usiok")
            {
                currentState.UsiOk(this);
            }
            else if (e.Data == "readyok")
            {
                currentState.ReadyOk(this);
            }
            else if (e.Data?.StartsWith("bestmove") ?? false)
            {
                currentState.Bestmove(e.Data!, this);
            }
        }

        public void SetOption()
        {
            currentState.SetOption(process!, this);
        }

        private void IsReady()
        {
            currentState.IsReady(process!, this);
        }

        public async Task IsReadyAsync()
        {
            IsReady();
            await ((AwaitingReadyOk)currentState).Tcs.Task;
        }

        public void StartNewGame()
        {
            currentState.StartNewGame(process!, this);
        }

        private void Quit()
        {
            currentState.Quit(process!, this);
        }

        public async Task QuitAsync()
        {
            Quit();
            await ((Quiting)currentState).Tcs.Task;
        }

        private void Go(Position pos, SearchLimit limits)
        {
            currentState.Go(process!, pos, limits, this);
        }

        public async Task<(Move, Move)> GoAsync(Position pos, SearchLimit limits)
        {
            Go(pos, limits);
            var (bestmove, ponder) = await ((BestmoveAwaitable)currentState).Tcs.Task;
            return (bestmove, ponder);
        }

        public void GoPonder(Position pos, SearchLimit limits)
        {
            currentState.GoPonder(process!, pos, limits, this);
        }

        public void NotifyPonderHit()
        {
            currentState.NotifyPonderHit(process!, this);
        }

        private void Stop()
        {
            currentState.Stop(process!, this);
        }

        public async Task StopAsync()
        {
            Stop();
            await ((BestmoveAwaitable)currentState).Tcs.Task;
        }

        public void Gameover(string message)
        {
            currentState.Gameover(process!, message, this);
        }
    }
}