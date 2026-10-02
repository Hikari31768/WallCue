using System;
using System.IO;
using System.Reflection;
using CountersPlus.Counters.Custom;
using CountersPlus.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace WallCue
{
    public sealed class WallHitsCounter : BasicCustomCounter, ILateTickable
    {
        [Inject(Optional = true)] private CollisionFeedback _feedback = null;
        private TMP_Text _title, _number;
        private int _shownCount = int.MinValue;
        private bool _wasAvailable;

        public override void CounterInit()
        {
            try
            {
                // Same sizes and vertical gap as Counters+ Counter.GenerateBasicText.
                _title = CanvasUtility.CreateTextFromSettings(Settings);
                _title.text = "Wall Hits";
                _title.fontSize = 3f;
                _title.color = Color.white;
                _title.raycastTarget = false;
                float positionScale = CanvasUtility.GetCanvasSettingsFromID(Settings.CanvasID).PositionScale;
                _number = CanvasUtility.CreateTextFromSettings(Settings, new Vector3(0f, -4f / positionScale, 0f));
                _number.fontSize = 4f;
                _number.color = Color.white;
                _number.raycastTarget = false;
                _number.text = "0";
                Plugin.Log.Info("Counters+ Wall Hits initialized; CanvasID=" + Settings.CanvasID + ", position=" + Settings.Position + ", distance=" + Settings.Distance);
            }
            catch (Exception e)
            {
                CounterDestroy();
                Plugin.Log.Error("Wall Hits display could not initialize: " + e);
            }
        }

        public void LateTick()
        {
            if (_number == null) return;
            bool available = _feedback != null && _feedback.Available;
            int count = available ? _feedback.Count : -1;
            if (count != _shownCount || available != _wasAvailable)
            {
                _number.text = available ? count.ToString() : "--";
                _shownCount = count;
                _wasAvailable = available;
            }
            _number.color = available && _feedback.CountIsRed(Time.time) ? Color.red : Color.white;
        }

        public override void CounterDestroy()
        {
            if (_title != null) UnityEngine.Object.Destroy(_title.gameObject);
            if (_number != null) UnityEngine.Object.Destroy(_number.gameObject);
            _title = _number = null;
            _shownCount = int.MinValue;
            _wasAvailable = false;
        }
    }

    public sealed class WallHitIconCounter : BasicCustomCounter, ILateTickable
    {
        [Inject(Optional = true)] private CollisionFeedback _feedback = null;
        private TMP_Text _anchor;
        private RawImage _image;
        private Texture2D _texture;

        public override void CounterInit()
        {
            try
            {
                // Use the same placement routine as every Counters+ text counter.
                // Keep its empty RectTransform as the anchor, so Canvas/Position/Distance
                // and custom HUD transforms apply to the icon too.
                _anchor = CanvasUtility.CreateTextFromSettings(Settings);
                _anchor.name = "WallCue Icon Anchor";
                _anchor.text = "";
                _anchor.enabled = false;

                byte[] data;
                using (Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream("WallCue.Resources.square_and_x.png"))
                {
                    if (source == null) throw new InvalidOperationException("Embedded collision icon is missing.");
                    using (var buffer = new MemoryStream())
                    {
                        source.CopyTo(buffer);
                        data = buffer.ToArray();
                    }
                }
                _texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                _texture.name = "WallCue Collision Icon";
                if (!ImageConversion.LoadImage(_texture, data, true)) throw new InvalidOperationException("Collision PNG could not be decoded.");
                _texture.wrapMode = TextureWrapMode.Clamp;
                _texture.filterMode = FilterMode.Bilinear;

                var go = new GameObject("Wall Hit Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                go.layer = _anchor.gameObject.layer;
                go.transform.SetParent(_anchor.rectTransform, false);
                _image = go.GetComponent<RawImage>();
                _image.texture = _texture;
                _image.material = ImagesUtility.NoGlowMaterial;
                _image.raycastTarget = false;
                _image.color = new Color(1f, 1f, 1f, 0f);
                _image.enabled = false;
                _image.rectTransform.anchorMin = _image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _image.rectTransform.anchoredPosition = Vector2.zero;
                _image.rectTransform.sizeDelta = new Vector2(7f, 7f);
                Plugin.Log.Info("Counters+ Wall Hit Icon initialized; embedded PNG=" + _texture.width + "x" + _texture.height + ", CanvasID=" + Settings.CanvasID + ", position=" + Settings.Position + ", distance=" + Settings.Distance);
            }
            catch (Exception e)
            {
                CounterDestroy();
                Plugin.Log.Error("Wall Hit Icon could not initialize: " + e);
            }
        }

        public void LateTick()
        {
            if (_image == null) return;
            float alpha = _feedback == null ? 0f : _feedback.IconAlpha(Time.time);
            _image.color = new Color(1f, 1f, 1f, alpha);
            _image.enabled = alpha > 0f;
        }

        public override void CounterDestroy()
        {
            // The image is an anchor child; shared Counters+ material is never destroyed.
            if (_anchor != null) UnityEngine.Object.Destroy(_anchor.gameObject);
            if (_texture != null) UnityEngine.Object.Destroy(_texture);
            _anchor = null;
            _image = null;
            _texture = null;
        }
    }
}
