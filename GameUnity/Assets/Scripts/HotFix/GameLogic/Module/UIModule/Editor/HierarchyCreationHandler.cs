#if UNITY_EDITOR

#if TextMeshPro
using TMPro;
#endif
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class HierarchyCreationHandler : Editor
    {
        private static CanvasScaler.ScaleMode UIWINDOW_DEFAULT_SCALE_MODE = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        private static int UIWINDOW_WIDTH = 1920;
        private static int UIWINDOW_HEIGHT = 1080;

        private static int m_lastProcessedInstanceId;

        [InitializeOnLoadMethod]
        private static void OnHierarchyCreateUIComponent()
        {
            // 监听Hierarchy窗口中的对象创建事件
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private static void OnHierarchyChanged()
        {
            OnComponentCreated();
            LoadUIWindowCamera();
        }

        private static void OnComponentCreated()
        {
            GameObject selectedObj = Selection.activeGameObject;

            if (selectedObj == null)
            {
                return;
            }

            // 避免重复处理同一个对象
            int instanceId = selectedObj.GetInstanceID();

            if (instanceId == m_lastProcessedInstanceId)
            {
                return;
            }

            #region 替换组件

#if TextMeshPro
            var tmpTextArr = selectedObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmpTextArr?.Length > 0)
            {
                for (int i = 0; i < tmpTextArr.Length; i++)
                {
                    tmpTextArr[i].raycastTarget = false;
                }
            }
#endif

            if (selectedObj.name == "Button (Legacy)"
                && selectedObj.TryGetComponent<Button>(out Button btn)
                && !selectedObj.TryGetComponent<UIButton>(out _))
            {
                m_lastProcessedInstanceId = instanceId;
                DestroyImmediate(btn.gameObject);
                UIButtonDrawEditor.CreateUIButton();
            }
            else if (selectedObj.name == "Image"
                     && selectedObj.TryGetComponent<Image>(out Image ig)
                     && !selectedObj.TryGetComponent<UIImage>(out _))
            {
                m_lastProcessedInstanceId = instanceId;
                DestroyImmediate(ig.gameObject);
                UIImageDrawEditor.CreateUIImage();
            }
            else if ((selectedObj.name == "Text (Legacy)") // || selectedObj.name == "Text"
                     && selectedObj.TryGetComponent<Text>(out Text txt)
                     && !selectedObj.TryGetComponent<UIText>(out _))
            {
                m_lastProcessedInstanceId = instanceId;
                DestroyImmediate(txt.gameObject);
                UITextDrawEditor.CreateUIText();
            }
            else if (selectedObj.name.Contains("Scroll View"))
            {
                m_lastProcessedInstanceId = instanceId;

                if (selectedObj.TryGetComponent<ScrollRect>(out var scrollRect))
                {
                    selectedObj.name = "m_scroll";
                }

                GameObject viewPort = selectedObj.transform.Find("Viewport").gameObject;

                if (viewPort.TryGetComponent<Mask>(out Mask mask))
                {
                    DestroyImmediate(mask);
                    viewPort.AddComponent<RectMask2D>();
                }

                if (viewPort.TryGetComponent<Image>(out Image viewPortImage))
                {
                    DestroyImmediate(viewPortImage);
                }

                Image[] images = selectedObj.GetComponentsInChildren<Image>(true);

                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imgColor = img.color;
                            var imageObj = img.gameObject;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                Scrollbar[] scrollbars = selectedObj.GetComponentsInChildren<Scrollbar>(true);

                if (scrollbars != null && scrollbars.Length > 0)
                {
                    foreach (var tmpScrollbar in scrollbars)
                    {
                        tmpScrollbar.transition = Selectable.Transition.None;
                    }
                }

                return;
            }
            else if (selectedObj.name.Contains("Slider"))
            {
                m_lastProcessedInstanceId = instanceId;

                if (selectedObj.TryGetComponent<Slider>(out var slider))
                {
                    selectedObj.name = "m_slider";
                    slider.transition = Selectable.Transition.None;
                }

                Image[] images = selectedObj.GetComponentsInChildren<Image>(true);

                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imageObj = img.gameObject;
                            var imgColor = img.color;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                return;
            }
            else if (selectedObj.name.Contains("Scrollbar"))
            {
                m_lastProcessedInstanceId = instanceId;

                if (selectedObj.TryGetComponent<Scrollbar>(out var scrollbar))
                {
                    selectedObj.name = "m_scrollbar";
                    scrollbar.transition = Selectable.Transition.None;
                }

                Image[] images = selectedObj.GetComponentsInChildren<Image>(true);

                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imgColor = img.color;
                            var imageObj = img.gameObject;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                return;
            }
            else if (selectedObj.name.Contains("InputField"))
            {
                m_lastProcessedInstanceId = instanceId;

                if (selectedObj.TryGetComponent<InputField>(out var inputField))
                {
                    selectedObj.name = "m_input";
                    inputField.transition = Selectable.Transition.None;
                }

                Image[] images = selectedObj.GetComponentsInChildren<Image>(true);

                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imgColor = img.color;
                            var imageObj = img.gameObject;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                Text[] texts = selectedObj.GetComponentsInChildren<Text>(true);

                if (texts != null && texts.Length > 0)
                {
                    foreach (var text in texts)
                    {
                        if (!(text is UIText))
                        {
                            var textContent = text.text;
                            var font = text.font;
                            var fontStyle = text.fontStyle;
                            var fontSize = text.fontSize;
                            var textColor = text.color;
                            var textRich = text.supportRichText;
                            var textHorizontalOverflow = text.horizontalOverflow;
                            var textVerticalOverflow = text.verticalOverflow;
                            var textObj = text.gameObject;

                            DestroyImmediate(text);
                            var uiText = textObj.AddComponent<UIText>();
                            uiText.raycastTarget = false;
                            uiText.text = textContent;
                            uiText.font = font;
                            uiText.color = textColor;
                            uiText.fontSize = fontSize;
                            uiText.fontStyle = fontStyle;
                            uiText.supportRichText = textRich;
                            uiText.horizontalOverflow = textHorizontalOverflow;
                            uiText.verticalOverflow = textVerticalOverflow;
                            uiText.raycastTarget = false;
                        }
                        else
                        {
                            text.raycastTarget = false;
                        }
                    }
                }

                var tmpTxt = selectedObj.GetComponentsInChildren<UIText>();

                if (tmpTxt != null && tmpTxt.Length > 0)
                {
                    inputField.placeholder = tmpTxt[0];
                    inputField.textComponent = tmpTxt[^1];
                }

                return;
            }
            else if (selectedObj.name.Contains("Dropdown"))
            {
                m_lastProcessedInstanceId = instanceId;

                if (selectedObj.TryGetComponent<Dropdown>(out var dropdown))
                {
                    selectedObj.name = "m_dropDown";
                    dropdown.transition = Selectable.Transition.None;
                }

                var maskArr = selectedObj.GetComponentsInChildren<Mask>(true);

                if (maskArr != null && maskArr.Length > 0)
                {
                    for (int i = 0; i < maskArr.Length; i++)
                    {
                        var tmpMask = maskArr[i];
                        var viewPort = tmpMask.gameObject;

                        // GameObject viewPort = obj.transform.Find("Viewport").gameObject;
                        if (viewPort.TryGetComponent<Mask>(out Mask mask))
                        {
                            DestroyImmediate(mask);
                            viewPort.AddComponent<RectMask2D>();
                        }

                        if (viewPort.TryGetComponent<Image>(out Image viewPortImage))
                        {
                            DestroyImmediate(viewPortImage);
                        }
                    }
                }

                Image[] images = selectedObj.GetComponentsInChildren<Image>(true);

                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imgColor = img.color;
                            var imageObj = img.gameObject;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                Text[] texts = selectedObj.GetComponentsInChildren<Text>(true);

                if (texts != null && texts.Length > 0)
                {
                    foreach (var text in texts)
                    {
                        if (!(text is UIText))
                        {
                            var textContent = text.text;
                            var font = text.font;
                            var fontStyle = text.fontStyle;
                            var fontSize = text.fontSize;
                            var textColor = text.color;
                            var textRich = text.supportRichText;
                            var textHorizontalOverflow = text.horizontalOverflow;
                            var textVerticalOverflow = text.verticalOverflow;
                            var textObj = text.gameObject;

                            DestroyImmediate(text);
                            var uiText = textObj.AddComponent<UIText>();
                            uiText.raycastTarget = false;
                            uiText.text = textContent;
                            uiText.font = font;
                            uiText.color = textColor;
                            uiText.fontSize = fontSize;
                            uiText.fontStyle = fontStyle;
                            uiText.supportRichText = textRich;
                            uiText.horizontalOverflow = textHorizontalOverflow;
                            uiText.verticalOverflow = textVerticalOverflow;
                            uiText.raycastTarget = false;
                        }
                        else
                        {
                            text.raycastTarget = false;
                        }
                    }
                }

                var scrollbars = selectedObj.GetComponentsInChildren<Scrollbar>(true);

                if (scrollbars != null && scrollbars.Length > 0)
                {
                    for (int i = 0; i < scrollbars.Length; i++)
                    {
                        var scrollbar = scrollbars[i];
                        scrollbar.transition = Selectable.Transition.None;
                    }
                }

                var toggles = selectedObj.GetComponentsInChildren<Toggle>(true);

                if (toggles != null && toggles.Length > 0)
                {
                    for (int i = 0; i < toggles.Length; i++)
                    {
                        var toggle = toggles[i];
                        toggle.transition = Selectable.Transition.None;
                        toggle.graphic = toggle.transform.Find("Item Checkmark").GetComponent<Image>();
                    }
                }

                Text[] tmpTexts = selectedObj.GetComponentsInChildren<Text>();
                dropdown.captionText = tmpTexts[0];

                return;
            }
            else if (selectedObj.name.Contains("Toggle"))
            {
                m_lastProcessedInstanceId = instanceId;

                if (selectedObj.TryGetComponent<Toggle>(out var toggle))
                {
                    selectedObj.name = "m_toggle";
                    toggle.transition = Selectable.Transition.None;
                }

                Image[] images = selectedObj.GetComponentsInChildren<Image>(true);

                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imgColor = img.color;
                            var imageObj = img.gameObject;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                Text[] texts = selectedObj.GetComponentsInChildren<Text>(true);

                if (texts != null && texts.Length > 0)
                {
                    foreach (var text in texts)
                    {
                        if (!(text is UIText))
                        {
                            var textContent = text.text;
                            var font = text.font;
                            var fontStyle = text.fontStyle;
                            var fontSize = text.fontSize;
                            var textColor = text.color;
                            var textRich = text.supportRichText;
                            var textHorizontalOverflow = text.horizontalOverflow;
                            var textVerticalOverflow = text.verticalOverflow;
                            var textObj = text.gameObject;

                            DestroyImmediate(text);
                            var uiText = textObj.AddComponent<UIText>();
                            uiText.raycastTarget = false;
                            uiText.text = textContent;
                            uiText.font = font;
                            uiText.color = textColor;
                            uiText.fontSize = fontSize;
                            uiText.fontStyle = fontStyle;
                            uiText.supportRichText = textRich;
                            uiText.horizontalOverflow = textHorizontalOverflow;
                            uiText.verticalOverflow = textVerticalOverflow;
                            uiText.raycastTarget = false;
                        }
                        else
                        {
                            text.raycastTarget = false;
                        }
                    }
                }

                Image[] tmpImages = selectedObj.GetComponentsInChildren<Image>(true);
                toggle.graphic = tmpImages[^1];
            }

            #endregion
        }

        private static void LoadUIWindowCamera()
        {
            var window = Selection.activeGameObject;

            if (window != null)
            {
                bool isUIWindow = window.name.ToUpper().Contains("UI")
                                  || window.name.Contains("Window");

                if (isUIWindow && window.TryGetComponent<Canvas>(out Canvas canvas))
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    GameObject uiCameraObj = GameObject.Find("UIRoot/UICamera");

                    if (uiCameraObj != null)
                    {
                        canvas.worldCamera = uiCameraObj.GetComponent<Camera>();
                    }

                    if (window.TryGetComponent<CanvasScaler>(out CanvasScaler canvasScaler))
                    {
                        canvasScaler.uiScaleMode = UIWINDOW_DEFAULT_SCALE_MODE;
                        canvasScaler.referenceResolution = new Vector2(UIWINDOW_WIDTH, UIWINDOW_HEIGHT);
                    }
                }
            }
        }
    }
}

#endif