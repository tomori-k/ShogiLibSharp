﻿using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ShogiLibSharp.Csa.Exceptions;

namespace ShogiLibSharp.Csa
{
    public class ShogiServerClient : CsaClient
    {
        public ShogiServerClient(
            IPlayerFactory playerFactory,
            ShogiServerOptions options,
            TimeSpan keepAliveInterval,
            ILogger<CsaClient> logger,
            CancellationToken ct = default)
            : base(playerFactory, options, keepAliveInterval, logger, ct)
        {
        }

        public ShogiServerClient(
            IPlayerFactory playerFactory,
            ShogiServerOptions options,
            ILogger<CsaClient> logger,
            CancellationToken ct = default)
            : base(playerFactory, options, logger, ct)
        {
        }

        public ShogiServerClient(
            IPlayerFactory playerFactory,
            ShogiServerOptions options,
            TimeSpan keepAliveInterval,
            CancellationToken ct = default)
            : base(playerFactory, options, keepAliveInterval, ct)
        {
        }

        public ShogiServerClient(
            IPlayerFactory playerFactory,
            ShogiServerOptions options,
            CancellationToken ct = default)
            : base(playerFactory, options, ct)
        {
        }

        async Task LoginAsync(CancellationToken ct)
        {
            await rw!.WriteLineAsync($"LOGIN {options.UserName} {options.Password} x1", ct)
                .ConfigureAwait(false);

            while (true)
            {
                var message = await rw.ReadLineAsync(ct).ConfigureAwait(false);
                if (message == $"##[LOGIN] +OK x1")
                {
                    return;
                }
                else if (message == "LOGIN:incorrect")
                {
                    throw new LoginFailedException("ログインできませんでした。");
                }
            }
        }

        private protected override async Task CommunicateWithServerAsync(CancellationToken ct)
        {
            await LoginAsync(ct).ConfigureAwait(false);

            while (true)
            {
                if (!playerFactory.ContinueLogin())
                {
                    Debug.Assert(!isWaitingForNextGame);
                    // ログアウト
                    await rw!.WriteLineAsync("LOGOUT", ct).ConfigureAwait(false);
                }

                await rw!.WriteLineAsync($"%%GAME {((ShogiServerOptions)options).GameName} *", ct)
                    .ConfigureAwait(false);

                bool accept;
                GameSummary? summary;
                try
                {
                    (summary, accept) = await ReceiveGameSummaryAsync(ct).ConfigureAwait(false);
                }
                catch (LogoutException)
                {
                    break;
                }

                // summary が null のときは、おかしな summary が送られてきたということなので、無視
                if (summary is null) continue;

                var player = accept
                    ? await playerFactory.AgreeWith(summary, ct).ConfigureAwait(false)
                    : null;

                if (player is null)
                {
                    await rw.WriteLineAsync($"REJECT", ct).ConfigureAwait(false);
                }
                else
                {
                    await rw.WriteLineAsync($"AGREE", ct).ConfigureAwait(false);
                }

                // Start or Reject が来るのを待つ
                if (!await ReceiveGameStartAsync(summary, ct).ConfigureAwait(false))
                {
                    playerFactory.Rejected(summary);
                    continue;
                }

                if (player is null) throw new CsaServerException("REJECT がサーバに無視されました;;");

                await new GameLoop(
                    rw!, summary, keepAliveInterval, player, options.SendPv)
                    .StartAsync(ct)
                    .ConfigureAwait(false);
            }
        }
    }
}
