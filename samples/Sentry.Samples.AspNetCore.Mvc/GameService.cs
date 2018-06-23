using System;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sentry.Samples.AspNetCore.Mvc
{
    public class GameService : IGameService
    {
        private readonly ILogger<GameService> _gameLogger;

        public GameService(ILogger<GameService> gameLogger) => _gameLogger = gameLogger;

        public async Task<(int dungeonsIds, int userMana)> FetchNextPhaseDataAsync()
        {
            _gameLogger.LogInformation("Fetching dungeons and mana level in parallel.");

            var getDungeonsTask = Task.Run(new Func<int>(() => throw new HttpRequestException("Failed to fetch available Dungeons")));
            var getUserMana = Task.Run(new Func<int>(() => throw new DataException("Invalid Mana level: -10")));

            var whenAll = Task.WhenAll(getDungeonsTask, getUserMana);
            try
            {
                var ids = await whenAll;
                return (ids[0], ids[1]);
            }
            // await unwraps AggregateException and throws the first one
            catch when (whenAll.Exception is AggregateException ae && ae.InnerExceptions.Count > 1)
            {
                throw ae; // re-throw the AggregateException to capture all errors
            }

        }
    }
}
