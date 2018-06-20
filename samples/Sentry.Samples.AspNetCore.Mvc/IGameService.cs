namespace Sentry.Samples.AspNetCore.Mvc
{
    public interface IGameService
    {
        System.Threading.Tasks.Task<(int dungeonsIds, int userMana)> FetchNextPhaseDataAsync();
    }
}