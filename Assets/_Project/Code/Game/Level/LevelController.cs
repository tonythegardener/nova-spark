using System.Collections.Generic;
using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NovaSpark.Game.Shooter;
using NovaSpark.Mods;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NovaSpark.Game.Level
{
    public sealed class LevelController : MonoBehaviour
    {
        private enum AppState { LevelSelect, Playing, Paused, Won, Lost }

        private AppState _state = AppState.LevelSelect;
        private LevelDefinition _level;
        private PathSampler _path;
        private MarbleChain _chain;
        private Cannon _shooter;
        private Sprite _circle;
        private Camera _camera;
        private Transform _levelRoot;

        private Vector3 _cameraHome;
        private float _shakeTimeLeft;
        private float _shakeAmplitude;

        // OnGUI cached styles
        private Texture2D _panelBg;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesReady;

        private void Start()
        {
            _circle = SpriteFactory.GetWhiteCircle();
            ConfigureCameraForMenu();
            _state = AppState.LevelSelect;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        // -------------------- camera --------------------

        private void ConfigureCameraForMenu()
        {
            _camera = EnsureCamera();
            _camera.orthographic = true;
            _camera.orthographicSize = 6f;
            _camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            _camera.transform.rotation = Quaternion.identity;
            _cameraHome = _camera.transform.position;
        }

        private void ConfigureCameraForLevel(LevelDefinition level)
        {
            _camera = EnsureCamera();
            _camera.orthographic = true;
            _camera.orthographicSize = level.CameraOrthographicSize;
            _camera.backgroundColor = level.BackgroundColor;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.transform.position = new Vector3(level.CameraCenter.x, level.CameraCenter.y, -10f);
            _camera.transform.rotation = Quaternion.identity;
            _cameraHome = _camera.transform.position;
        }

        private static Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (cam != null) return cam;
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            return go.AddComponent<Camera>();
        }

        // -------------------- level lifecycle --------------------

        private void LoadLevel(string contentId)
        {
            TearDownLevel();
            var def = ContentRegistry.Get<LevelDefinition>(contentId);
            if (def == null)
            {
                Debug.LogError($"[NovaSpark] LevelController could not find level '{contentId}' in registry.");
                return;
            }

            _level = def;
            _path = new PathSampler(_level.Waypoints);
            ConfigureCameraForLevel(_level);

            _levelRoot = new GameObject("LevelRoot").transform;
            _levelRoot.SetParent(transform, false);

            BuildPathVisual();
            BuildBastionMarker();
            BuildChain();
            BuildCannon();

            _state = AppState.Playing;
            Time.timeScale = 1f;
            Debug.Log($"[NovaSpark] Level '{_level.DisplayName}' started: path length {_path.TotalLength:F2}, " +
                      $"speed {_level.ChainAdvanceSpeed}, marbles {_level.TotalMarbles}");
        }

        private void TearDownLevel()
        {
            if (_levelRoot != null)
            {
                Destroy(_levelRoot.gameObject);
                _levelRoot = null;
            }
            _chain = null;
            _shooter = null;
            _path = null;
            _level = null;
        }

        private void GoToLevelSelect()
        {
            TearDownLevel();
            ConfigureCameraForMenu();
            _state = AppState.LevelSelect;
            Time.timeScale = 1f;
        }

        // -------------------- builders --------------------

        private void BuildPathVisual()
        {
            var go = new GameObject("PathVisual");
            go.transform.SetParent(_levelRoot, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = _path.Points.Count;
            for (int i = 0; i < _path.Points.Count; i++)
            {
                line.SetPosition(i, _path.Points[i]);
            }
            line.startWidth = _level.MarbleRadius * 2.05f;
            line.endWidth = _level.MarbleRadius * 2.05f;
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.13f, 0.16f, 0.22f, 1f);
            line.material = mat;
            line.sortingOrder = -10;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
        }

        private void BuildBastionMarker()
        {
            var bastionPos = _path.Points[_path.Points.Count - 1];
            var go = new GameObject("Bastion");
            go.transform.SetParent(_levelRoot, false);
            go.transform.position = bastionPos;
            go.transform.localScale = Vector3.one * (_level.MarbleRadius * 3.2f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _circle;
            sr.color = new Color(0.85f, 0.85f, 0.95f, 1f);
            sr.sortingOrder = -5;
        }

        private void BuildChain()
        {
            var chainGo = new GameObject("MarbleChain");
            chainGo.transform.SetParent(_levelRoot, false);
            _chain = chainGo.AddComponent<MarbleChain>();
            _chain.Configure(_level, _path, _circle);
            _chain.OnReachedBastion += OnLost;
            _chain.OnCleared += OnWon;
            _chain.OnMatchResolved += () => ShakeCamera(0.18f, 0.15f);
        }

        private void BuildCannon()
        {
            var shooterGo = new GameObject("Cannon");
            shooterGo.transform.SetParent(_levelRoot, false);
            _shooter = shooterGo.AddComponent<Cannon>();
            _shooter.Configure(_level, _chain, _camera, _circle);
        }

        // -------------------- update / input --------------------

        private void Update()
        {
            HandleEscape();

            if (_state == AppState.Playing && _chain != null && _level != null)
            {
                _chain.Tick(Time.deltaTime);
            }
            UpdateCameraShake();
        }

        private void HandleEscape()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb.escapeKey.wasPressedThisFrame) return;

            switch (_state)
            {
                case AppState.Playing:
                    Pause();
                    break;
                case AppState.Paused:
                    Resume();
                    break;
                case AppState.Won:
                case AppState.Lost:
                    GoToLevelSelect();
                    break;
                case AppState.LevelSelect:
                    // no-op; user can click Quit instead
                    break;
            }
        }

        private void Pause()
        {
            if (_state != AppState.Playing) return;
            _state = AppState.Paused;
            Time.timeScale = 0f;
            if (_shooter != null) _shooter.SetFiringEnabled(false);
        }

        private void Resume()
        {
            if (_state != AppState.Paused) return;
            _state = AppState.Playing;
            Time.timeScale = 1f;
            if (_shooter != null) _shooter.SetFiringEnabled(true);
        }

        // -------------------- end states --------------------

        private void OnWon()
        {
            if (_state != AppState.Playing) return;
            _state = AppState.Won;
            if (_shooter != null) _shooter.SetFiringEnabled(false);
        }

        private void OnLost()
        {
            if (_state != AppState.Playing) return;
            _state = AppState.Lost;
            if (_shooter != null) _shooter.SetFiringEnabled(false);
        }

        // -------------------- camera shake --------------------

        private void ShakeCamera(float duration, float amplitude)
        {
            _shakeTimeLeft = Mathf.Max(_shakeTimeLeft, duration);
            _shakeAmplitude = Mathf.Max(_shakeAmplitude, amplitude);
        }

        private void UpdateCameraShake()
        {
            if (_camera == null) return;
            if (_shakeTimeLeft <= 0f)
            {
                _camera.transform.position = _cameraHome;
                _shakeAmplitude = 0f;
                return;
            }
            _shakeTimeLeft -= Time.deltaTime;
            float t = Mathf.Max(0f, _shakeTimeLeft);
            float amp = _shakeAmplitude * (t / 0.2f);
            var offset = new Vector3(
                (Random.value - 0.5f) * 2f * amp,
                (Random.value - 0.5f) * 2f * amp,
                0f);
            _camera.transform.position = _cameraHome + offset;
        }

        // -------------------- OnGUI --------------------

        private void OnGUI()
        {
            EnsureStyles();
            switch (_state)
            {
                case AppState.LevelSelect: DrawLevelSelect(); break;
                case AppState.Paused:      DrawPauseMenu();   break;
                case AppState.Won:         DrawEndScreen(true);  break;
                case AppState.Lost:        DrawEndScreen(false); break;
            }
        }

        private void DrawLevelSelect()
        {
            var levels = new List<LevelDefinition>(ContentRegistry.All<LevelDefinition>());
            levels.Sort((a, b) => string.CompareOrdinal(a.ContentId, b.ContentId));

            int rowH = 56;
            int rowGap = 8;
            int contentH = 110 + levels.Count * (rowH + rowGap) + 70;
            int contentW = 520;
            var boxRect = CenteredBox(contentW, contentH);

            GUI.Box(boxRect, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(boxRect.x, boxRect.y + 22, boxRect.width, 50),
                "NOVA SPARK", _titleStyle);
            GUI.Label(new Rect(boxRect.x, boxRect.y + 68, boxRect.width, 30),
                "Select a sector", _bodyStyle);

            int y = (int)boxRect.y + 110;
            foreach (var lv in levels)
            {
                var label = $"{lv.DisplayName}\n<size=12>{lv.ContentId}</size>";
                var btnRect = new Rect(boxRect.x + 30, y, boxRect.width - 60, rowH);
                if (GUI.Button(btnRect, label, _buttonStyle))
                {
                    LoadLevel(lv.ContentId);
                }
                y += rowH + rowGap;
            }

            var quitRect = new Rect(boxRect.x + boxRect.width / 2f - 80, y + 8, 160, 44);
            if (GUI.Button(quitRect, "Quit", _buttonStyle))
            {
                QuitApp();
            }
        }

        private void DrawPauseMenu()
        {
            int w = 440, h = 260;
            var boxRect = CenteredBox(w, h);
            GUI.Box(boxRect, GUIContent.none, _panelStyle);

            var titleColor = new Color(0.85f, 0.90f, 0.95f);
            DrawTitle(boxRect, "Paused", titleColor);

            int btnW = 220, btnH = 44, gap = 10;
            int by = (int)boxRect.y + 95;
            int bx = (int)boxRect.x + (int)((boxRect.width - btnW) / 2);

            if (GUI.Button(new Rect(bx, by, btnW, btnH), "Resume", _buttonStyle))
            {
                Resume();
            }
            by += btnH + gap;
            if (GUI.Button(new Rect(bx, by, btnW, btnH), "Restart Level", _buttonStyle))
            {
                var id = _level != null ? _level.ContentId : null;
                if (!string.IsNullOrEmpty(id)) LoadLevel(id);
            }
            by += btnH + gap;
            if (GUI.Button(new Rect(bx, by, btnW, btnH), "Quit to Level Select", _buttonStyle))
            {
                GoToLevelSelect();
            }
        }

        private void DrawEndScreen(bool won)
        {
            int w = 460, h = 240;
            var boxRect = CenteredBox(w, h);
            GUI.Box(boxRect, GUIContent.none, _panelStyle);

            var title = won ? "Sector cleared" : "Bastion overrun";
            var color = won
                ? new Color(0.55f, 0.95f, 0.65f)
                : new Color(0.95f, 0.55f, 0.55f);
            DrawTitle(boxRect, title, color);

            int btnW = 220, btnH = 44, gap = 10;
            int by = (int)boxRect.y + 95;
            int bx = (int)boxRect.x + (int)((boxRect.width - btnW) / 2);
            if (GUI.Button(new Rect(bx, by, btnW, btnH), "Replay Level", _buttonStyle))
            {
                if (_level != null) LoadLevel(_level.ContentId);
            }
            by += btnH + gap;
            if (GUI.Button(new Rect(bx, by, btnW, btnH), "Level Select", _buttonStyle))
            {
                GoToLevelSelect();
            }
        }

        // -------------------- gui helpers --------------------

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _panelBg = MakeSolid(new Color(0f, 0f, 0f, 0.85f));
            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = _panelBg;
            _panelStyle.padding = new RectOffset(20, 20, 20, 20);

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                richText = true,
            };
            _titleStyle.normal.textColor = Color.white;

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                richText = true,
            };
            _bodyStyle.normal.textColor = new Color(0.7f, 0.75f, 0.85f, 1f);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
            };
            _stylesReady = true;
        }

        private static Texture2D MakeSolid(Color c)
        {
            var tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }

        private static Rect CenteredBox(int width, int height)
        {
            return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        }

        private void DrawTitle(Rect boxRect, string title, Color color)
        {
            var style = new GUIStyle(_titleStyle);
            style.normal.textColor = color;
            GUI.Label(new Rect(boxRect.x, boxRect.y + 28, boxRect.width, 50), title, style);
        }

        // -------------------- quit --------------------

        private static void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
