using System;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sentry.Samples.AspNetCore.Mvc
{
    public class GameService : IGameService
    {
        public async Task<(int dungeonsIds, int userMana)> FetchNextPhaseDataAsync()
        {
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
                throw ae; // re-throw the AggregateException
            }

        }
    }
}
