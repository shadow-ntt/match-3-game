using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace DG.Tweening
{
    // Cung cap extension method ToUniTask cho cac Tween cua DOTween
    public static class TweenExtensions
    {
        public static UniTask ToUniTask(this Tween tween)
        {
            if (tween == null || !tween.IsActive() || tween.IsComplete())
            {
                return UniTask.CompletedTask;
            }

            var utcs = new UniTaskCompletionSource();
            tween.OnComplete(() => utcs.TrySetResult());
            tween.OnKill(() => utcs.TrySetResult());
            return utcs.Task;
        }
    }
}
