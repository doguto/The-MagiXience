using System;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Project.Scripts.Extensions
{
    public static class ObservableExtensions
    {
        /// <summary>
        /// asyncActionの実行中に発火したイベントを無視し、ボタンの二重クリック等による多重起動を防ぐ。
        /// </summary>
        public static IDisposable SubscribeBlocking<T>(this IObservable<T> source, Func<T, UniTask> asyncAction)
        {
            var isExecuting = false;

            return source.Subscribe(x =>
            {
                if (isExecuting) return;
                isExecuting = true;
                Execute(x).Forget();
            });

            async UniTaskVoid Execute(T value)
            {
                try
                {
                    await asyncAction(value);
                }
                finally
                {
                    isExecuting = false;
                }
            }
        }
    }
}
