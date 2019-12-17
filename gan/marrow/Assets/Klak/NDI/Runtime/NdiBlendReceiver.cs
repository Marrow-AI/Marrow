// KlakNDI - NDI plugin for Unity
// https://github.com/keijiro/KlakNDI

using UnityEngine;
using System.Collections;

namespace Klak.Ndi
{
    [ExecuteInEditMode]
    [AddComponentMenu("Marrow/NDI Blend Receiver")]
    public sealed class NdiBlendReceiver : MonoBehaviour
    {
        #region Source settings

        [SerializeField] string _sourceName;

        public string sourceName {
            get { return _sourceName; }
            set {
                if (_sourceName == value) return;
                _sourceName = value;
                RequestReconnect();
            }
        }

        #endregion

        #region Target settings

        [SerializeField] RenderTexture _targetTextureOne;
        [SerializeField] RenderTexture _targetTextureTwo;
        [SerializeField] RenderTexture _targetTextureThree;


        public RenderTexture targetTextureOne {
            get { return _targetTextureOne; }
            set { _targetTextureOne = value; }
        }
        public RenderTexture targetTextureTwo {
            get { return _targetTextureTwo; }
            set { _targetTextureTwo = value; }
        }
        public RenderTexture targetTextureThree {
            get { return _targetTextureThree; }
            set { _targetTextureThree = value; }
        }

        [SerializeField] float _FPS;
        [SerializeField] float _FPS2;

        public float FPS {
            get { return _FPS; }
            set { _FPS = value; }
        }
        public float FPS2 {
            get { return _FPS2; }
            set { _FPS2 = value; }
        }

        [SerializeField] Renderer _targetRenderer;

        public Renderer targetRenderer {
            get { return _targetRenderer; }
            set { _targetRenderer = value; }
        }

        [SerializeField] string _targetMaterialProperty = null;

        public string targetMaterialProperty {
            get { return _targetMaterialProperty; }
            set { _targetMaterialProperty = value; }
        }

        #endregion

        #region Runtime properties

        RenderTexture _receivedTexture;
        RenderTexture _frontTexture;
        RenderTexture _backTexture;

        public Texture receivedTexture {
            get { return _targetTextureOne != null ? _targetTextureOne : _receivedTexture; }
        }

        #endregion

        #region Private members

        static System.IntPtr _callback;

        System.IntPtr _plugin;
        Texture2D _sourceTexture;
        Material _blendMaterial;
        MaterialPropertyBlock _propertyBlock;

        float _dataTime;

        bool _showFalse;
        float _timer;
        float _timer2;
        float _dataTime2;
        bool _showBack;
        float _blendFactor;

        #endregion

        #region Internal members

        internal void RequestReconnect()
        {
            OnDisable();
        }

        #endregion

        #region MonoBehaviour implementation

        void OnDisable()
        {
            if (_plugin != System.IntPtr.Zero)
            {
                PluginEntry.DestroyReceiver(_plugin);
                _plugin = System.IntPtr.Zero;
            }
        }
        void OnEnable()
        {
            _FPS = 0.5f;
            _FPS2 = 2.0f;

        }

        void OnDestroy()
        {
            Util.Destroy(_blendMaterial);
            Util.Destroy(_sourceTexture);
            Util.Destroy(_frontTexture);
            Util.Destroy(_backTexture);
        }

        void Awake() {
            _FPS = 0.5f;
            _FPS2 = 2.0f;
            _dataTime = 1.0f / _FPS;
            _dataTime2 = 1.0f / _FPS2;
            _showBack = true;
            _timer = 0.0f;
            _timer2 = 0.0f;

        }

        void Update() {

            if (!PluginEntry.IsAvailable) return;

            _dataTime = 1.0f / _FPS;
            _dataTime2 = 1.0f / _FPS2;


            // Plugin lazy initialization
            if (_plugin == System.IntPtr.Zero) {
                _plugin = PluginEntry.CreateReceiver(_sourceName);
                if (_plugin == System.IntPtr.Zero) return; // No receiver support
            }

            // Texture update event invocation with lazy initialization
            if (_callback == System.IntPtr.Zero)
                _callback = PluginEntry.GetTextureUpdateCallback();

            if (_sourceTexture == null) {
                _sourceTexture = new Texture2D(8, 8); // Placeholder
                _sourceTexture.hideFlags = HideFlags.DontSave;
            }

            Util.IssueTextureUpdateEvent
                (_callback, _sourceTexture, PluginEntry.GetReceiverID(_plugin));

            // Texture information retrieval
            var width = PluginEntry.GetFrameWidth(_plugin);
            var height = PluginEntry.GetFrameHeight(_plugin);
            if (width == 0 || height == 0) return; // Not yet ready

            // Source data dimensions
            var alpha = PluginEntry.GetFrameFourCC(_plugin) == FourCC.UYVA;
            var sw = width / 2;
            var sh = height * (alpha ? 3 : 2) / 2;

            // Renew the textures when the dimensions are changed.
            if (_sourceTexture.width != sw || _sourceTexture.height != sh) {
                Util.Destroy(_sourceTexture);
                Util.Destroy(_receivedTexture);
                _sourceTexture = new Texture2D(sw, sh, TextureFormat.RGBA32, false, true);
                _sourceTexture.hideFlags = HideFlags.DontSave;
                _sourceTexture.filterMode = FilterMode.Point;
            }

            // Receiver texture lazy initialization
            if (_targetTextureOne == null && _receivedTexture == null) {
                _receivedTexture = new RenderTexture(width, height, 0);
                _receivedTexture.hideFlags = HideFlags.DontSave;
            }

            if (_backTexture == null) {
                _backTexture = new RenderTexture(width, height, 0);
                _backTexture.hideFlags = HideFlags.DontSave;
            }
            if (_frontTexture == null) {
                _frontTexture = new RenderTexture(width, height, 0);
                _frontTexture.hideFlags = HideFlags.DontSave;
            }
            if (_blendMaterial == null) {
                _blendMaterial = new Material(Shader.Find("Hidden/Marrow/BlendReceiver"));
                _blendMaterial.hideFlags = HideFlags.DontSave;
                _blendMaterial.SetTexture("_BackTex", _backTexture);
            }

            _timer += Time.deltaTime;
            _timer2 += Time.deltaTime;

            if (_timer > _dataTime) {
                _showBack = !_showBack;
                _timer = 0.0f;
                Debug.Log("Blit!!");
                if (_showBack) {
                    Graphics.Blit(_sourceTexture, _frontTexture, _blendMaterial, 0);
                } else {
                    Graphics.Blit(_sourceTexture, _backTexture, _blendMaterial, 0);
                }
            }
            if (!_showBack) {
                _blendFactor = _timer / _dataTime;
            } else {
                _blendFactor = 1 - (_timer / _dataTime);
            }
            _blendMaterial.SetFloat("_BlendFactor", _blendFactor);
            Graphics.Blit(_frontTexture, _targetTextureOne, _blendMaterial, 1);
            Graphics.Blit(_frontTexture, _targetTextureThree, _blendMaterial, 3);
            if (_timer2 > _dataTime2) {
                _timer2 = 0;
                Graphics.Blit(_sourceTexture, _targetTextureTwo, _blendMaterial, 2);
                _targetTextureTwo.IncrementUpdateCount();

            }

            // Texture format conversion using the blit shader
            _targetTextureOne.IncrementUpdateCount();
            _targetTextureThree.IncrementUpdateCount();


            // Renderer override
            if (_targetRenderer != null)
            {
                // Material property block lazy initialization
                if (_propertyBlock == null)
                    _propertyBlock = new MaterialPropertyBlock();

                // Read-modify-write
                _targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetTexture(_targetMaterialProperty, _targetTextureOne);
                _propertyBlock.SetTexture(_targetMaterialProperty, _targetTextureTwo);
                _propertyBlock.SetTexture(_targetMaterialProperty, _targetTextureThree);
                _targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        #if UNITY_EDITOR

        // Invoke update on repaint in edit mode. This is needed to update the
        // shared texture without getting the object marked dirty.

        void OnRenderObject()
        {
            if (Application.isPlaying) return;

            // Graphic.Blit used in Update will change the current active RT,
            // so let us back it up and restore after Update.
            var activeRT = RenderTexture.active;
            Update();
            RenderTexture.active = activeRT;
        }

        #endif

        #endregion
    }
}
