using ShogiLibSharp.Core;
using ShogiLibSharp.Engine.State;
using System.Diagnostics;

namespace ShogiLibSharp.Engine
{
    public class UsiEngine
    {
        private Process? process;
        private StateBase currentState = new Deactivated();

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

            lock (currentState)
            {
                currentState = new AwaitingUsiOk();
            }

            await ((AwaitingUsiOk)currentState).Tcs.Task;
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            lock (currentState)
            {
                currentState.Exited(ref currentState);
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == "usiok")
            {
                lock (currentState)
                {
                    currentState.UsiOk(ref currentState);
                }
            }
            else if (e.Data == "readyok")
            {
                lock (currentState)
                {
                    currentState.ReadyOk(ref currentState);
                }
            }
            else if (e.Data?.StartsWith("bestmove") ?? false)
            {
                lock (currentState)
                {
                    currentState.Bestmove(e.Data!, ref currentState);
                }
            }
        }

        public void SetOption()
        {
            lock (currentState)
            {
                currentState.SetOption(process!, ref currentState);
            }
        }

        private void IsReady()
        {
            lock (currentState)
            {
                currentState.IsReady(process!, ref currentState);
            }
        }

        public async Task IsReadyAsync()
        {
            IsReady();
            await ((AwaitingReadyOk)currentState).Tcs.Task;
        }

        public void StartNewGame()
        {
            lock (currentState)
            {
                currentState.StartNewGame(process!, ref currentState);
            }
        }

        private void Quit()
        {
            lock (currentState)
            {
                currentState.Quit(process!, ref currentState);
            }
        }

        public async Task QuitAsync()
        {
            Quit();
            await ((Quiting)currentState).Tcs.Task;
        }

        private void Go(Position pos, SearchLimit limits)
        {
            lock (currentState)
            {
                currentState.Go(process!, pos, limits, ref currentState);
            }
        }

        public async Task<(Move, Move)> GoAsync(Position pos, SearchLimit limits)
        {
            Go(pos, limits);
            var (bestmove, ponder) = await ((BestmoveAwaitable)currentState).Tcs.Task;
            return (bestmove, ponder);
        }

        public void GoPonder(Position pos, SearchLimit limits)
        {
            lock (currentState)
            {
                currentState.GoPonder(process!, pos, limits, ref currentState);
            }
        }

        public void NotifyPonderHit()
        {
            lock (currentState)
            {
                currentState.NotifyPonderHit(process!, ref currentState);
            }
        }

        private void Stop()
        {
            lock (currentState)
            {
                currentState.Stop(process!, ref currentState);
            }
        }

        public async Task StopAsync()
        {
            Stop();
            await ((BestmoveAwaitable)currentState).Tcs.Task;
        }

        public void Gameover(string message)
        {
            lock (currentState)
            {
                currentState.Gameover(process!, message, ref currentState);
            }
        }
    }
}