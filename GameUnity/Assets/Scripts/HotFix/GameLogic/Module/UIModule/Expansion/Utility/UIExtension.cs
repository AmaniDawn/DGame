using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public static class UIExtension
    {
        #region SetActive

        public static void SetActive(this GameObject go, bool value, ref bool cacheValue)
        {
            if (go != null && value != cacheValue)
            {
                cacheValue = value;
                go.SetActive(value);
            }
        }

        public static void SetActive(this Image image, bool value, ref bool cacheValue)
        {
            if (image != null && value != cacheValue)
            {
                cacheValue = value;
                image.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Button btn, bool value, ref bool cacheValue)
        {
            if (btn != null && value != cacheValue)
            {
                cacheValue = value;
                btn.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Text text, bool value, ref bool cacheValue)
        {
            if (text != null && value != cacheValue)
            {
                cacheValue = value;
                text.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this InputField input, bool value, ref bool cacheValue)
        {
            if (input != null && value != cacheValue)
            {
                cacheValue = value;
                input.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this TMP_InputField input, bool value, ref bool cacheValue)
        {
            if (input != null && value != cacheValue)
            {
                cacheValue = value;
                input.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this ScrollRect scroll, bool value, ref bool cacheValue)
        {
            if (scroll != null && value != cacheValue)
            {
                cacheValue = value;
                scroll.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Slider slider, bool value, ref bool cacheValue)
        {
            if (slider != null && value != cacheValue)
            {
                cacheValue = value;
                slider.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Scrollbar scrollbar, bool value, ref bool cacheValue)
        {
            if (scrollbar != null && value != cacheValue)
            {
                cacheValue = value;
                scrollbar.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Dropdown dropdown, bool value, ref bool cacheValue)
        {
            if (dropdown != null && value != cacheValue)
            {
                cacheValue = value;
                dropdown.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this TMP_Dropdown dropdown, bool value, ref bool cacheValue)
        {
            if (dropdown != null && value != cacheValue)
            {
                cacheValue = value;
                dropdown.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this RectTransform rect, bool value, ref bool cacheValue)
        {
            if (rect != null && value != cacheValue)
            {
                cacheValue = value;
                rect.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Transform transform, bool value, ref bool cacheValue)
        {
            if (transform != null && value != cacheValue)
            {
                cacheValue = value;
                transform.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Toggle toggle, bool value, ref bool cacheValue)
        {
            if (toggle != null && value != cacheValue)
            {
                cacheValue = value;
                toggle.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Canvas canvas, bool value, ref bool cacheValue)
        {
            if (canvas != null && value != cacheValue)
            {
                cacheValue = value;
                canvas.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this CanvasGroup canvasGroup, bool value, ref bool cacheValue)
        {
            if (canvasGroup != null && value != cacheValue)
            {
                cacheValue = value;
                canvasGroup.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Image image, bool value)
        {
            if (image != null && image.gameObject.activeSelf != value)
            {
                image.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this RichTextItem richTextItem, bool value)
        {
            if (richTextItem != null && richTextItem.gameObject.activeSelf != value)
            {
                richTextItem.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Button btn, bool value)
        {
            if (btn != null && btn.gameObject.activeSelf != value)
            {
                btn.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Text text, bool value)
        {
            if (text != null && text.gameObject.activeSelf != value)
            {
                text.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this TextMeshProUGUI text, bool value)
        {
            if (text != null && text.gameObject.activeSelf != value)
            {
                text.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this InputField input, bool value)
        {
            if (input != null && input.gameObject.activeSelf != value)
            {
                input.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this TMP_InputField input, bool value)
        {
            if (input != null && input.gameObject.activeSelf != value)
            {
                input.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this ScrollRect scroll, bool value)
        {
            if (scroll != null && scroll.gameObject.activeSelf != value)
            {
                scroll.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Slider slider, bool value)
        {
            if (slider != null && slider.gameObject.activeSelf != value)
            {
                slider.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Scrollbar scrollbar, bool value)
        {
            if (scrollbar != null && scrollbar.gameObject.activeSelf != value)
            {
                scrollbar.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Dropdown dropdown, bool value)
        {
            if (dropdown != null && dropdown.gameObject.activeSelf != value)
            {
                dropdown.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this TMP_Dropdown dropdown, bool value)
        {
            if (dropdown != null && dropdown.gameObject.activeSelf != value)
            {
                dropdown.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this RectTransform rect, bool value)
        {
            if (rect != null && rect.gameObject.activeSelf != value)
            {
                rect.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Transform transform, bool value)
        {
            if (transform != null && transform.gameObject.activeSelf != value)
            {
                transform.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Toggle toggle, bool value)
        {
            if (toggle != null && toggle.gameObject.activeSelf != value)
            {
                toggle.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this Canvas canvas, bool value)
        {
            if (canvas != null && canvas.gameObject.activeSelf != value)
            {
                canvas.gameObject.SetActive(value);
            }
        }

        public static void SetActive(this CanvasGroup canvasGroup, bool value)
        {
            if (canvasGroup != null && canvasGroup.gameObject.activeSelf != value)
            {
                canvasGroup.gameObject.SetActive(value);
            }
        }

        #endregion

        #region UniTask

        public static UniTask<CanvasGroup> FadeToAlphaAsync(this CanvasGroup canvasGroup, float targetAlpha, float duration,
            Action callback = null, CancellationToken cancellationToken = default,
            DGame.Utility.EaseType easeType = DGame.Utility.EaseType.Linear)
        {
            return DGame.Utility.EaseUtil.FadeToAlphaAsync(canvasGroup, targetAlpha, duration, callback, cancellationToken, easeType, false);
        }

        public static UniTask<CanvasGroup> FadeToAlphaAsyncUnscaled(this CanvasGroup canvasGroup, float targetAlpha, float duration,
            Action callback = null, CancellationToken cancellationToken = default,
            DGame.Utility.EaseType easeType = DGame.Utility.EaseType.Linear)
        {
            return DGame.Utility.EaseUtil.FadeToAlphaAsync(canvasGroup, targetAlpha, duration, callback, cancellationToken, easeType, true);
        }

        public static UniTask<CanvasGroup> FadeInAsync(this CanvasGroup canvasGroup, float duration = 0.3f,
            Action callback = null, CancellationToken cancellationToken = default)
        {
            return FadeToAlphaAsyncUnscaled(canvasGroup,1f, duration, callback, cancellationToken);
        }

        public static UniTask<CanvasGroup> FadeOutAsync(this CanvasGroup canvasGroup, float duration = 0.3f,
            Action callback = null, CancellationToken cancellationToken = default)
        {
            return FadeToAlphaAsyncUnscaled(canvasGroup, 0f, duration, callback, cancellationToken);
        }

        public static UniTask<Slider> SmoothValue(this Slider slider, float targetValue, float duration = 0.3f,
            Action callback = null, CancellationToken cancellationToken = default,
            DGame.Utility.EaseType easeType = DGame.Utility.EaseType.Linear)
        {
            return DGame.Utility.EaseUtil.SmoothValue(slider, targetValue, duration, callback, cancellationToken,
                easeType);
        }

        public static UniTask<Image> SmoothValue(this Image image, float targetValue, float duration = 0.3f,
            Action callback = null, CancellationToken cancellationToken = default)
        {
            return DGame.Utility.EaseUtil.SmoothValue(image, targetValue, duration, callback, cancellationToken);
        }

        public static UniTask<Scrollbar> SmoothValue(this Scrollbar scrollbar, float targetValue, float duration = 0.3f,
            Action callback = null, CancellationToken cancellationToken = default)
        {
            return DGame.Utility.EaseUtil.SmoothValue(scrollbar, targetValue, duration, callback, cancellationToken);
        }

        public static bool TryGetMouseDownUIPos(this UIModule uiModule, out Vector2 screenPos)
        {
            return DGame.Utility.MathUtil.TryGetMouseDownUIPos((RectTransform)UIModule.UICanvas, uiModule.UICamera, out screenPos);
        }

        #endregion

        #region UIText多语言

        // public static string Convert(this TextDefine textDefine)
        // {
        //     return TextDefineConverter.Convert(textDefine);
        // }

        #endregion
    }
}
