using System.Threading;
using Cysharp.Threading.Tasks;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    public interface IDeathDirector
    {
        UniTask PlayAsync(CancellationToken ct);
    }
}
