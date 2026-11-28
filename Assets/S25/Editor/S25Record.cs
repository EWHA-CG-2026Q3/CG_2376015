#if UNITY_EDITOR
// ---------------------------------------------------------------------------
//  S25 과제 2 영상 녹화 (Unity Recorder, Game View -> MP4)
//  메뉴: CG > S25 영상 녹화 (35초)
//  Play를 시작하고 35초 동안 Game 뷰를 녹화한 뒤 Play를 끝낸다.
//  파일: 프로젝트 폴더/S25_Video/S25_Shield.mp4  (Assets 밖)
//  녹화 중에는 실드를 직접 클릭하고, Inspector에서 Mat_S25_Shield_MissionA의 Opacity를 0까지 내린다.
//  (Game 뷰만 녹화되므로 실드 이름표에 각 재질의 현재 Opacity 값을 표시한다)
// ---------------------------------------------------------------------------
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

public static class S25Record
{
    const string MARKER = "Assets/S25/.runrecord";
    const string M_A = "Assets/S25/Mat_S25_Shield_MissionA.mat";
    const float DURATION = 36f;
    const string FLAG = "S25Record.Pending";

    static RecorderController controller;
    static double startTime;

    [DidReloadScripts]
    static void AutoRun()
    {
        if (!File.Exists(MARKER)) return;
        File.Delete(MARKER);
        EditorApplication.delayCall += Begin;
    }

    [MenuItem("CG/S25 영상 녹화 (35초)", false, 3)]
    public static void Begin()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(M_A);
        mat.SetFloat("_Opacity", 0.5f);
        Selection.activeObject = mat;              // Inspector에 Mission A 재질을 띄워 둠
        SessionState.SetBool(FLAG, true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    static void Hook()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    static void OnPlayMode(PlayModeStateChange s)
    {
        if (s != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(FLAG, false)) return;
        SessionState.SetBool(FLAG, false);

        // 이름표에 Opacity 표시
        foreach (var lbl in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
        {
            if (lbl.name != "Label" || lbl.transform.parent == null) continue;
            var c = lbl.gameObject.AddComponent<S25_OpacityLabel>();
            c.target = lbl.transform.parent.GetComponent<Renderer>();
            c.title = lbl.text;
        }

        var cs = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "S25 Shield";
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
        };
        movie.CaptureAudio = false;
        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
        Directory.CreateDirectory("S25_Video");
        movie.OutputFile = Path.GetFullPath("S25_Video/S25_Shield");
        cs.AddRecorderSettings(movie);
        cs.SetRecordModeToManual();
        cs.FrameRate = 30f;
        cs.CapFrameRate = true;

        controller = new RecorderController(cs);
        controller.PrepareRecording();
        controller.StartRecording();
        startTime = EditorApplication.timeSinceStartup;
        File.WriteAllText("S25_Video/_record_log.txt", "녹화 시작 " + System.DateTime.Now.ToString("HH:mm:ss") + "\n");
        Debug.Log("[S25] 녹화 시작 (" + DURATION + "초)");
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (controller == null) { EditorApplication.update -= Tick; return; }
        if (EditorApplication.timeSinceStartup - startTime < DURATION && EditorApplication.isPlaying) return;
        EditorApplication.update -= Tick;
        controller.StopRecording();
        controller = null;
        File.AppendAllText("S25_Video/_record_log.txt", "녹화 끝 " + System.DateTime.Now.ToString("HH:mm:ss") + "\n");
        Debug.Log("[S25] 녹화 끝: S25_Video/S25_Shield.mp4");
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () =>
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(M_A);
            mat.SetFloat("_Opacity", 0.5f);   // 녹화 뒤 원래 값으로
            AssetDatabase.SaveAssets();
        };
    }
}
#endif
