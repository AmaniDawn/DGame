using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DGame
{
    public static partial class Utility
    {
        public static class EaseUtil
        {
            public static async UniTask<CanvasGroup> FadeToAlphaAsync(CanvasGroup canvasGroup, float targetAlpha,
                float duration, Action callback = null, CancellationToken cancellationToken = default,
                EaseType easeType = EaseType.Linear, bool ignoreTimeScale = false)
            {
                if (canvasGroup == null)
                {
                    return null;
                }

                if (duration <= 0f)
                {
                    canvasGroup.alpha = targetAlpha;
                    callback?.Invoke();
                    return canvasGroup;
                }

                float startAlpha = canvasGroup.alpha;
                float elapsedTime = 0f;

                try
                {
                    while (elapsedTime < duration)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return canvasGroup;
                        }

                        elapsedTime += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsedTime / duration);
                        float easedProgress = Evaluate(easeType, progress);
                        canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);
                        var timing = ignoreTimeScale ? PlayerLoopTiming.LastPostLateUpdate : PlayerLoopTiming.Update;
                        await UniTask.Yield(timing, cancellationToken);
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        canvasGroup.alpha = targetAlpha;
                        callback?.Invoke();
                    }

                    return canvasGroup;
                }
                catch (OperationCanceledException)
                {
                    return canvasGroup;
                }
            }

            public static async UniTask<Slider> SmoothValue(Slider slider, float targetValue,
                float duration, Action callback = null, CancellationToken cancellationToken = default,
                EaseType easeType = EaseType.Linear, bool ignoreTimeScale = false)
            {
                if (slider == null)
                {
                    return null;
                }

                if (duration <= 0f)
                {
                    slider.value = targetValue;
                    callback?.Invoke();
                    return slider;
                }

                float startValue = slider.value;
                float elapsedTime = 0f;

                try
                {
                    while (elapsedTime < duration)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return slider;
                        }

                        elapsedTime += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsedTime / duration);
                        float easedProgress = Evaluate(easeType, progress);
                        slider.value = Mathf.Lerp(startValue, targetValue, easedProgress);
                        var timing = ignoreTimeScale ? PlayerLoopTiming.LastPostLateUpdate : PlayerLoopTiming.Update;
                        await UniTask.Yield(timing, cancellationToken);
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        slider.value = targetValue;
                        callback?.Invoke();
                    }

                    return slider;
                }
                catch (OperationCanceledException)
                {
                    return slider;
                }
            }

            public static async UniTask<Image> SmoothValue(Image image, float targetValue,
                float duration, Action callback = null, CancellationToken cancellationToken = default,
                EaseType easeType = EaseType.Linear, bool ignoreTimeScale = false)
            {
                if (image == null)
                {
                    return null;
                }

                if (duration <= 0f)
                {
                    image.fillAmount = targetValue;
                    callback?.Invoke();
                    return image;
                }

                float startValue = image.fillAmount;
                float elapsedTime = 0f;

                try
                {
                    while (elapsedTime < duration)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return image;
                        }

                        elapsedTime += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsedTime / duration);
                        float easedProgress = Evaluate(easeType, progress);
                        image.fillAmount = Mathf.Lerp(startValue, targetValue, easedProgress);
                        var timing = ignoreTimeScale ? PlayerLoopTiming.LastPostLateUpdate : PlayerLoopTiming.Update;
                        await UniTask.Yield(timing, cancellationToken);
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        image.fillAmount = targetValue;
                        callback?.Invoke();
                    }

                    return image;
                }
                catch (OperationCanceledException)
                {
                    return image;
                }
            }

            public static async UniTask<Scrollbar> SmoothValue(Scrollbar scrollbar, float targetValue,
                float duration, Action callback = null, CancellationToken cancellationToken = default,
                EaseType easeType = EaseType.Linear, bool ignoreTimeScale = false)
            {
                if (scrollbar == null)
                {
                    return null;
                }

                if (duration <= 0f)
                {
                    scrollbar.value = targetValue;
                    callback?.Invoke();
                    return scrollbar;
                }

                float startValue = scrollbar.value;
                float elapsedTime = 0f;

                try
                {
                    while (elapsedTime < duration)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return scrollbar;
                        }

                        elapsedTime += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsedTime / duration);
                        float easedProgress = Evaluate(easeType, progress);
                        scrollbar.value = Mathf.Lerp(startValue, targetValue, easedProgress);
                        var timing = ignoreTimeScale ? PlayerLoopTiming.LastPostLateUpdate : PlayerLoopTiming.Update;
                        await UniTask.Yield(timing, cancellationToken);
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        scrollbar.value = targetValue;
                        callback?.Invoke();
                    }

                    return scrollbar;
                }
                catch (OperationCanceledException)
                {
                    return scrollbar;
                }
            }

            public static async UniTaskVoid SmoothValue(float from, float to,
                float duration, Action<float> callback = null, CancellationToken cancellationToken = default,
                EaseType easeType = EaseType.Linear, bool ignoreTimeScale = false)
            {
                if (duration <= 0f)
                {
                    from = to;
                    callback?.Invoke(from);
                    return;
                }

                float startValue = from;
                float elapsedTime = 0f;

                try
                {
                    while (elapsedTime < duration)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        elapsedTime += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsedTime / duration);
                        float easedProgress = Evaluate(easeType, progress);
                        var curValue = Mathf.Lerp(startValue, to, easedProgress);
                        callback?.Invoke(curValue);
                        var timing = ignoreTimeScale ? PlayerLoopTiming.LastPostLateUpdate : PlayerLoopTiming.Update;
                        await UniTask.Yield(timing, cancellationToken);
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        from = to;
                        callback?.Invoke(from);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }

            public static float Evaluate(EaseType easeType, float progress)
            {
                return easeType switch
                {
                    EaseType.Linear => progress,
                    EaseType.EaseInQuad => EaseInQuad(progress),
                    EaseType.EaseOutQuad => EaseOutQuad(progress),
                    EaseType.EaseInOutQuad => EaseInOutQuad(progress),
                    _ => progress
                };
            }

            private static float EaseInQuad(float t) => t * t;
            private static float EaseOutQuad(float t) => 1 - (1 - t) * (1 - t);
            private static float EaseInOutQuad(float t) => t < 0.5 ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) * 0.5f;
        }

        public enum EaseType
        {
            /// <summary>
            /// 线性
            /// </summary>
            Linear,

            /// <summary>
            /// 从静止开始，逐渐加速
            /// </summary>
            EaseInQuad,

            /// <summary>
            /// 快速开始，逐渐减速到停止
            /// </summary>
            EaseOutQuad,

            /// <summary>
            /// 开始慢→中间快→结束慢
            /// </summary>
            EaseInOutQuad
        }
    }
}