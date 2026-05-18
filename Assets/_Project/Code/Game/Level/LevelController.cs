using NovaSpark.Core;
using NovaSpark.Game.Chain;
using NovaSpark.Game.Shooter;
using NovaSpark.Mods;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaSpark.Game.Level
{
    public sealed class LevelController : MonoBehaviour
    {
        [Tooltip("Content id of the level to load. Must be present in the ContentRegistry at Start.")]
        [SerializeField] private string startLevelContentId = "level.phase0b_smoke_test";

        private LevelDefinition _level;
        private PathSampler _path;
        private MarbleChain _chain;
        private Cannon _shooter;
        private Sprite _circle;
        private LineRenderer _pathLine;
        private Camera _camera;

        private enum EndState { Playing, Won, Lost }
        private EndState _endState = EndState.Playing;

        private Vector3 _cameraHome;
        private float _shakeTimeLeft;
        private float _shakeAmplitude;

        private void Start()
        {
            _level = ContentRegistry.Get<LevelDefinition>(startLevelContentId);
            if (_level == null)
            {
                Debug.LogError($"[NovaSpark] LevelController could not find level '{startLevelContentId}' in registry.");
                return;
            }
            _circle = SpriteFactory.GetWhiteCircle();
            _camera = ConfigureCamera(_level);
            _cameraHome = _camera.transform.position;
            _path = new PathSampler(_level.Waypoints);

            BuildPathVisual();
            BuildBastionMarker();
            BuildChain();
            BuildShooter();

            Debug.Log($"[NovaSpark] Level '{_level.DisplayName}' started: path length {_path.TotalLength:F2}, " +
                      $"speed {_level.ChainAdvanceSpeed}, marbles {_level.TotalMarbles}");
        }

        private Camera ConfigureCamera(LevelDefinition level)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = level.CameraOrthographicSize;
            cam.backgroundColor = level.BackgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(level.CameraCenter.x, level.CameraCenter.y, -10f);
            cam.transform.rotation = Quaternion.identity;
            return cam;
        }

        private void BuildPathVisual()
        {
            var go = new GameObject("PathVisual");
            go.transform.SetParent(transform, false);
            _pathLine = go.AddComponent<LineRenderer>();
            _pathLine.useWorldSpace = true;
            _pathLine.positionCount = _path.Points.Count;
            for (int i = 0; i < _path.Points.Count; i++)
            {
                _pathLine.SetPosition(i, _path.Points[i]);
            }
            _pathLine.startWidth = _level.MarbleRadius * 2.05f;
            _pathLine.endWidth = _level.MarbleRadius * 2.05f;
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.13f, 0.16f, 0.22f, 1f);
            _pathLine.material = mat;
            _pathLine.sortingOrder = -10;
            _pathLine.numCornerVertices = 4;
            _pathLine.numCapVertices = 4;
        }

        private void BuildBastionMarker()
        {
            var bastionPos = _path.Points[_path.Points.Count - 1];
            var go = new GameObject("Bastion");
            go.transform.SetParent(transform, false);
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
            chainGo.transform.SetParent(transform, false);
            _chain = chainGo.AddComponent<MarbleChain>();
            _chain.Configure(_level, _path, _circle);
            _chain.OnReachedBastion += OnLost;
            _chain.OnCleared += OnWon;
            _chain.OnMatchResolved += () => ShakeCamera(0.18f, 0.15f);
        }

        private void BuildShooter()
        {
            var shooterGo = new GameObject("Shooter");
            shooterGo.transform.SetParent(transform, false);
            _shooter = shooterGo.AddComponent<Cannon>();
            _shooter.Configure(_level, _chain, _camera, _circle);
        }

        private void Update()
        {
            if (_endState == EndState.Playing && _chain != null && _level != null)
            {
                _chain.Tick(Time.deltaTime);
            }
            UpdateCameraShake();
        }

        private void OnWon()
        {
            if (_endState != EndState.Playing) return;
            _endState = EndState.Won;
            if (_shooter != null) _shooter.SetFiringEnabled(false);
        }

        private void OnLost()
        {
            if (_endState != EndState.Playing) return;
            _endState = EndState.Lost;
            if (_shooter != null) _shooter.SetFiringEnabled(false);
        }

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

        private void OnGUI()
        {
            if (_endState == EndState.Playing) return;

            var w = Screen.width;
            var h = Screen.height;
            var boxRect = new Rect(w / 2f - 220, h / 2f - 90, 440, 180);

            var bgStyle = new GUIStyle(GUI.skin.box);
            bgStyle.normal.background = MakeSolid(new Color(0f, 0f, 0f, 0.78f));
            bgStyle.normal.textColor = Color.white;
            GUI.Box(boxRect, GUIContent.none, bgStyle);

            var title = _endState == EndState.Won ? "Sector cleared" : "Bastion overrun";
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
            };
            titleStyle.normal.textColor = _endState == EndState.Won
                ? new Color(0.55f, 0.95f, 0.65f)
                : new Color(0.95f, 0.55f, 0.55f);
            GUI.Label(new Rect(boxRect.x, boxRect.y + 18, boxRect.width, 50), title, titleStyle);

            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            if (GUI.Button(new Rect(boxRect.x + 130, boxRect.y + 100, 180, 50), "Restart", buttonStyle))
            {
                Restart();
            }
        }

        private static Texture2D MakeSolid(Color c)
        {
            var tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }

        private void Restart()
        {
            var active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }
    }
}
