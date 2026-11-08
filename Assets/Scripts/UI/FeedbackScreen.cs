using System.Collections;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Platform;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: FEEDBACK-001] Feedback from inside the game (the owner's pick K3 after 0.4.2): F8 anywhere, or "Send feedback"
    /// in the run's Options. The screen is taken before the form opens and shown on it; the player picks what kind of note
    /// it is, writes it (Steam's keyboard on a Deck), may add a Discord name, and ticks what goes with it: the screenshot,
    /// the run's details, the log, the save. "What is sent" shows the report as it will go. FeedbackSender sends it or
    /// writes a ZIP. Nothing goes without a tick; the save is unticked at first.
    /// </summary>
    public sealed class FeedbackScreen : MonoBehaviour
    {
        /// <summary>Open, and also the frame it closed in, so that frame's Escape reaches no other screen.</summary>
        public static bool IsOpen => _open || Time.frameCount == _closedFrame;
        private static bool _open;
        private static int _closedFrame = -1;
        private static FeedbackScreen _instance;

        private Canvas _canvas;
        private GameObject _root;
        private byte[] _shot;
        private Texture2D _shotTexture;
        private string _kind = FeedbackReport.Kinds[0];
        private InputField _text, _discord;
        private Toggle _withShot, _withRun, _withLog, _withSave;
        private Text _preview, _status;
        private Button _send;
        private readonly Dictionary<string, Image> _kindPills = new Dictionary<string, Image>();
        private bool _sending;

        private void Awake() => _instance = this;

        // [REQ: CRASH-REPORT-001] Offered once after a game that did not close properly, when the menu is up. Only the player's
        // Send lets a report leave; before that the form shows exactly what it holds.
        private bool _crashReport;

        private IEnumerator Start()
        {
            if (!SudokuRoguelike.Platform.SessionGuard.LastSessionCrashed) yield break;
            while (SudokuRoguelike.Bootstrap.SplashScreenController.Showing
                   || FindAnyObjectByType<FirstRunSetupModalController>() != null) yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            if (!IsOpen) ShowCrashPrompt();
        }

        private void ShowCrashPrompt()
        {
            _open = true;
            EnsureCanvas();
            var layer = ScrollUiKit.Fill(_canvas.transform, "CrashPromptLayer", Vector2.zero, Vector2.one, new Color(0.04f, 0.03f, 0.02f, 0.80f));
            layer.raycastTarget = true;
            _root = layer.gameObject;
            var paper = ScrollUiKit.Fill(layer.transform, "CrashPromptPaper", new Vector2(0.28f, 0.32f), new Vector2(0.72f, 0.68f), ScrollUiKit.Paper).transform;
            ScrollUiKit.Fill(paper, "Band", new Vector2(0f, 0.975f), Vector2.one, ScrollUiKit.Vermilion);
            ScrollUiKit.Label(paper, "Title", T("Feedback.Crash.Title", "The game did not close properly"), 34, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.06f, 0.70f), new Vector2(0.94f, 0.92f), 20);
            ScrollUiKit.Label(paper, "Body", T("Feedback.Crash.Body", "A report helps to find the cause. You see all of it first, and nothing leaves your PC unless you press Send."),
                22, TextAnchor.UpperLeft, ScrollUiKit.InkSoft, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.68f), 14);
            var look = ScrollUiKit.PillButton(paper, "BtnCrashLook", new Vector2(0.06f, 0.07f), new Vector2(0.56f, 0.25f),
                T("Feedback.Crash.Look", "Look at the report"), 22, ScrollUiKit.Vermilion, Color.white, 22f);
            look.onClick.AddListener(() =>
            {
                ScrollUiKit.DestroyNow(_root);
                _root = null;
                _crashReport = true;
                Build();
            });
            var later = ScrollUiKit.PillButton(paper, "BtnCrashLater", new Vector2(0.60f, 0.07f), new Vector2(0.94f, 0.25f),
                T("Feedback.Crash.Later", "Not now"), 22, ScrollUiKit.Wood, ScrollUiKit.PaperText, 22f);
            later.onClick.AddListener(Close);
            look.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = later };
            later.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = look };
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(look.gameObject);
        }

        private void Update()
        {
            if (!IsOpen && Input.GetKeyDown(KeyCode.F8)) Open();
            else if (IsOpen && _root != null && Input.GetKeyDown(KeyCode.Escape) && !_sending) Close();
        }

        /// <summary>Opens the form (from a button); the screenshot is taken first.</summary>
        public static void Open()
        {
            if (_instance != null && !IsOpen) _instance.StartCoroutine(_instance.CaptureThenOpen());
        }

        private IEnumerator CaptureThenOpen()
        {
            _open = true;   // keys go to the form from now on
            // The frame is drawn first, so the shot shows the game without the form; batch mode draws no frame (and has no shot).
            if (Application.isBatchMode) yield return null;
            else yield return new WaitForEndOfFrame();
            if (!_open) yield break;
            if (!Application.isBatchMode) CaptureShot();
            Build();
        }

        private void CaptureShot()
        {
            try
            {
                _shotTexture = Shrink(ScreenCapture.CaptureScreenshotAsTexture(), 1280);   // a 4K shot would not fit the relay's 2 MB
                _shot = _shotTexture.EncodeToJPG(80);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Feedback] No screenshot: {e.Message}");   // the form opens without one
                _shotTexture = null;
                _shot = null;
            }
        }

        private static Texture2D Shrink(Texture2D full, int maxWidth)
        {
            if (full.width <= maxWidth) return full;
            var w = maxWidth;
            var h = Mathf.RoundToInt(full.height * (maxWidth / (float)full.width));
            var rt = RenderTexture.GetTemporary(w, h);
            Graphics.Blit(full, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var small = new Texture2D(w, h, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            small.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(full);
            return small;
        }

        private static RunDirector CurrentRun()
        {
            var map = FindAnyObjectByType<RunMapController>();
            return map != null && map.isActiveAndEnabled && map.gameObject.activeInHierarchy ? map.Run : null;
        }

        private void EnsureCanvas()
        {
            if (_canvas != null) return;
            var go = new GameObject("FeedbackCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 900;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void Build()
        {
            EnsureCanvas();
            var layer = ScrollUiKit.Fill(_canvas.transform, "FeedbackLayer", Vector2.zero, Vector2.one, new Color(0.04f, 0.03f, 0.02f, 0.80f));
            layer.raycastTarget = true;
            _root = layer.gameObject;
            var paper = ScrollUiKit.Fill(layer.transform, "FeedbackPaper", new Vector2(0.16f, 0.07f), new Vector2(0.84f, 0.93f), ScrollUiKit.Paper);
            var p = paper.transform;
            ScrollUiKit.Fill(p, "Band", new Vector2(0f, 0.985f), Vector2.one, ScrollUiKit.Vermilion);
            ScrollUiKit.Label(p, "Eyebrow", T("Feedback.Eyebrow", "TO THE DEVELOPER").ToUpperInvariant(), 17, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.04f, 0.92f), new Vector2(0.60f, 0.96f), 11);
            ScrollUiKit.Label(p, "Title", T("Feedback.Title", "Send feedback"), 42, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.04f, 0.85f), new Vector2(0.60f, 0.92f), 24);

            for (var i = 0; i < FeedbackReport.Kinds.Length; i++)
            {
                var kind = FeedbackReport.Kinds[i];
                var x = 0.04f + i * 0.14f;
                var pill = ScrollUiKit.PillButton(p, "Kind_" + kind, new Vector2(x, 0.77f), new Vector2(x + 0.13f, 0.83f),
                    T("Feedback.Kind." + kind, kind), 20, ScrollUiKit.Wood, ScrollUiKit.PaperText, 20f);
                pill.onClick.AddListener(() => ChooseKind(kind));
                _kindPills[kind] = pill.GetComponent<Image>();
            }

            _text = Field(p, "FeedbackText", new Vector2(0.04f, 0.42f), new Vector2(0.60f, 0.75f), 22,
                T("Feedback.Placeholder", "What happened? What did you expect?"), FeedbackReport.MaxTextLength, true);
            if (_crashReport) _text.text = T("Feedback.Crash.Text", "The game did not close properly the last time. What was I doing:");
            _discord = Field(p, "FeedbackDiscord", new Vector2(0.04f, 0.34f), new Vector2(0.60f, 0.40f), 20,
                T("Feedback.Discord", "Your Discord name (optional), so I can answer"), 40, false);

            if (_shotTexture != null)
            {
                var raw = new GameObject("Screenshot", typeof(RectTransform), typeof(RawImage));
                raw.transform.SetParent(p, false);
                var rt = (RectTransform)raw.transform;
                rt.anchorMin = new Vector2(0.63f, 0.60f);
                rt.anchorMax = new Vector2(0.96f, 0.83f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                raw.GetComponent<RawImage>().texture = _shotTexture;
                raw.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                raw.GetComponent<AspectRatioFitter>().aspectRatio = _shotTexture.width / (float)_shotTexture.height;
            }

            ScrollUiKit.Label(p, "WithHeader", T("Feedback.With", "SEND WITH IT").ToUpperInvariant(), 16, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.63f, 0.53f), new Vector2(0.96f, 0.57f), 10);
            _withShot = Tick(p, "WithShot", 0.47f, T("Feedback.With.Shot", "The screenshot"), !_crashReport);
            _withRun = Tick(p, "WithRun", 0.41f, T("Feedback.With.Run", "The run: seed, board, class, floor"), !_crashReport && CurrentRun() != null);
            _withLog = Tick(p, "WithLog", 0.35f, T("Feedback.With.Log", "The game's log (last 200 lines)"), true);
            _withSave = Tick(p, "WithSave", 0.29f, T("Feedback.With.Save", "My save file, to load the run"), false);

            ScrollUiKit.Label(p, "PreviewHeader", T("Feedback.Preview", "WHAT IS SENT").ToUpperInvariant(), 16, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.04f, 0.27f), new Vector2(0.60f, 0.31f), 10);
            _preview = ScrollUiKit.Label(p, "Preview", string.Empty, 15, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.04f, 0.11f), new Vector2(0.96f, 0.27f), 10);
            _preview.supportRichText = false;
            ScrollUiKit.Label(p, "Privacy", T("Feedback.Privacy", "Sent: your note, the version, device and language, a random ID of this installation, and what is ticked. No name, no Steam ID."),
                15, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.04f, 0.03f), new Vector2(0.50f, 0.09f), 10);
            _status = ScrollUiKit.Label(p, "Status", string.Empty, 18, TextAnchor.MiddleRight, ScrollUiKit.Vermilion,
                new Vector2(0.30f, 0.09f), new Vector2(0.96f, 0.12f), 11);

            _send = ScrollUiKit.PillButton(p, "BtnFeedbackSend", new Vector2(0.52f, 0.025f), new Vector2(0.74f, 0.09f),
                T("Feedback.Send", "Send"), 24, ScrollUiKit.Vermilion, Color.white, 24f);
            _send.onClick.AddListener(Send);
            var cancel = ScrollUiKit.PillButton(p, "BtnFeedbackCancel", new Vector2(0.76f, 0.025f), new Vector2(0.96f, 0.09f),
                T("Feedback.Cancel", "Cancel"), 22, ScrollUiKit.Wood, ScrollUiKit.PaperText, 24f);
            cancel.onClick.AddListener(() => { if (!_sending) Close(); });

            foreach (var toggle in new[] { _withShot, _withRun, _withLog, _withSave })
                toggle.onValueChanged.AddListener(_ => RefreshPreview());
            _text.onValueChanged.AddListener(_ => RefreshPreview());
            ChooseKind(_kind);
            EventSystem.current?.SetSelectedGameObject(_kindPills[_kind].gameObject);
        }

        private void ChooseKind(string kind)
        {
            _kind = kind;
            foreach (var pair in _kindPills)
                if (pair.Value != null) pair.Value.color = pair.Key == kind ? ScrollUiKit.Vermilion : ScrollUiKit.Wood;
            RefreshPreview();
        }

        private string ReportText(RunDirector run) => FeedbackReport.Build(_kind, _text != null ? _text.text : string.Empty,
            _discord != null ? _discord.text : string.Empty, "v" + Application.version, Device(), LocalizationService.Current.ToString(),
            InputDeviceTracker.UsingPad, run, _withRun != null && _withRun.isOn);

        private void RefreshPreview()
        {
            if (_preview == null) return;
            var lines = ReportText(CurrentRun()).Split('\n');
            _preview.text = string.Join("\n", lines, 0, Mathf.Min(lines.Length, 9)) + (lines.Length > 9 ? "\n…" : string.Empty)
                            + "\n" + Attachments();
        }

        private string Attachments()
        {
            var parts = new List<string>();
            if (_withShot != null && _withShot.isOn) parts.Add("screenshot.jpg");
            if (_withLog != null && _withLog.isOn) parts.Add("log.txt");
            if (_withSave != null && _withSave.isOn) parts.Add("save.json");
            return parts.Count > 0 ? "+ " + string.Join(", ", parts) : string.Empty;
        }

        private void Send()
        {
            if (_sending) return;
            if (FeedbackSender.TooSoon) { _status.text = T("Feedback.TooSoon", "One moment: one report a minute."); return; }
            var run = CurrentRun();
            var report = new FeedbackSender.Report
            {
                Kind = _kind,
                Title = FeedbackReport.Title(_kind, "v" + Application.version, _withRun.isOn ? run : null),
                Text = ReportText(run),
                Screenshot = _withShot.isOn ? _shot : null,
                Log = _withLog.isOn ? FeedbackReport.LogTail(_crashReport ? SudokuRoguelike.Platform.SessionGuard.PreviousLogPath : Application.consoleLogPath) : null,
                Save = _withSave.isOn ? ReadSave() : null
            };
            _sending = true;
            _send.interactable = false;
            _status.text = T("Feedback.Sending", "Sending…");
            StartCoroutine(FeedbackSender.Send(report, (sent, zip) =>
            {
                _sending = false;
                if (_root == null) return;   // closed meanwhile
                if (sent)
                {
                    Close();
                    return;
                }
                _send.interactable = true;
                if (zip == null) { _status.text = T("Feedback.Failed", "It could not be sent or saved."); return; }
                if (run?.State != null) GUIUtility.systemCopyBuffer = run.State.Seed.ToString();
                _status.text = LocalizationService.Format("Feedback.Saved", "Saved as {0}: post it in #bug-reports on Discord.",
                    System.IO.Path.GetFileName(zip));
                var folder = System.IO.Path.GetDirectoryName(zip);
                if (!string.IsNullOrEmpty(folder)) Application.OpenURL(new System.Uri(folder).AbsoluteUri);
            }));
        }

        private static string ReadSave()
        {
            var path = SaveFileService.GetSlotPath(SaveProfileService.ActiveSlot);
            try { return System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : null; }
            catch (System.IO.IOException) { return null; }
        }

        private static string Device() =>
            SteamDeck.Running ? "Steam Deck" : SystemInfo.operatingSystem + " · " + SystemInfo.deviceModel;

        private void Close()
        {
            if (_root != null) ScrollUiKit.DestroyNow(_root);
            _root = null;
            if (_shotTexture != null) Destroy(_shotTexture);
            _shotTexture = null;
            _shot = null;
            _kindPills.Clear();
            _crashReport = false;
            _open = false;
            _closedFrame = Time.frameCount;
        }

        private static InputField Field(Transform parent, string name, Vector2 min, Vector2 max, int size, string hint, int limit, bool multiLine)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(GamepadKeyboardInputField));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var box = go.GetComponent<Image>();
            box.sprite = ScrollUiKit.Rounded(10f);
            box.type = Image.Type.Sliced;
            box.color = new Color(1f, 0.98f, 0.94f, 1f);
            ScrollUiKit.InkFrame(go.transform, new Color(0.33f, 0.29f, 0.24f, 0.45f), 1.5f);
            var text = ScrollUiKit.Label(go.transform, "Text", string.Empty, size, multiLine ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft,
                ScrollUiKit.Ink, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f));
            text.supportRichText = false;
            var placeholder = ScrollUiKit.Label(go.transform, "Placeholder", hint, size, multiLine ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft,
                new Color(0.42f, 0.38f, 0.33f, 0.8f), new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f));
            placeholder.fontStyle = FontStyle.Italic;
            var field = go.GetComponent<InputField>();
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = limit;
            field.lineType = multiLine ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            return field;
        }

        private static Toggle Tick(Transform parent, string name, float y, string label, bool on)
        {
            var row = ScrollUiKit.Area(parent, name, new Vector2(0.63f, y), new Vector2(0.96f, y + 0.05f));
            var hit = row.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.01f);
            var toggle = row.gameObject.AddComponent<Toggle>();
            var box = ScrollUiKit.Fill(row, "Box", new Vector2(0f, 0.15f), new Vector2(0.07f, 0.85f), new Color(1f, 0.98f, 0.94f, 1f));
            ScrollUiKit.InkFrame(box.transform, ScrollUiKit.Ink, 2f);
            var check = ScrollUiKit.Fill(box.transform, "Check", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), ScrollUiKit.Vermilion);
            ScrollUiKit.Label(row, "Label", label, 19, TextAnchor.MiddleLeft, ScrollUiKit.Ink, new Vector2(0.10f, 0f), Vector2.one, 11);
            toggle.targetGraphic = hit;
            toggle.graphic = check;
            toggle.isOn = on;
            return toggle;
        }

        private static string T(string key, string fallback) => LocalizationService.T(key, fallback);
    }
}
