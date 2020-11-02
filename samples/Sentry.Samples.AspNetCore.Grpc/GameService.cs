using System;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Sentry.AspNetCore.Samples.Grpc;

namespace Sentry.Samples.AspNetCore.Grpc
{
    public class GameService : Games.GamesBase
    {
        private readonly ILogger _gameLogger;

        public GameService(ILogger<GameService> gameLogger) => _gameLogger = gameLogger;

        public override async Task<NextPhaseResponse> FetchNextPhaseData(Empty request, ServerCallContext context)
        {
            _gameLogger.LogInformation("Fetching dungeons and mana level in parallel.");

            var getDungeonsTask =
                Task.Run(new Func<int>(() => throw new HttpRequestException("Failed to fetch available Dungeons")));
            var getUserMana = Task.Run(new Func<int>(() => throw new DataException("Invalid Mana level: -10")));

            var whenAll = Task.WhenAll(getDungeonsTask, getUserMana);
            try
            {
                var ids = await whenAll;

                var response = new NextPhaseResponse { DungeonsIds = ids[0], UserMana = ids[1] };

                return response;
            }
            // await unwraps AggregateException and throws the first one
            catch when (whenAll.Exception is { } ae && ae.InnerExceptions.Count > 1)
            {
                throw ae; // re-throw the AggregateException to capture all errors
            }
        }
    }
}
