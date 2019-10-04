using UnityEditor;
using UnityEngine;
using System;
using System.IO;

public class MenuTest : MonoBehaviour
{
    private static string buildLogFile = null;

    static void WriteLog(string s)
    {
        try
        {
            StreamWriter sw = new StreamWriter(buildLogFile, true);
            sw.WriteLine(s);
            sw.Close();
        }
        catch (Exception)
        {
        }
    }
    static string FormatBytes(ulong bytes)
    {
        double gb = bytes / (1024.0 * 1024.0 * 1024.0);
        double mb = bytes / (1024.0 * 1024.0);
        double kb = bytes / 1024.0;
        if (mb >= 1000) return gb.ToString("#.000") + "GB";
        if (kb >= 1000) return mb.ToString("#.000") + "MB";
        if (kb >= 1) return kb.ToString("#.000") + "KB";
        return bytes + "B";
    }

    static string TimeSpanToHHMMSSString(TimeSpan s)
    {
        if (s.ToString("hh") != "00")
            return s.ToString("hh") + "h " + s.ToString("mm") + "m " + s.ToString("ss") + "s";
        else
            return s.ToString("mm") + "m " + s.ToString("ss") + "s";
    }

    static string PathRelativeTo(string path, string basePathRelativeTo)
    {
        // Abuse URI computation to compute a path relative to another
        return new Uri(basePathRelativeTo + "/").MakeRelativeUri(new Uri(path)).ToString();
    }

    static void DoHtml5BuildToDirectory(string path, string emscriptenLinkerFlags, WebGLCompressionFormat compressionFormat)
    {
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm; // WebGLLinkerTarget.Asm;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.memorySize = 256;
        PlayerSettings.WebGL.emscriptenArgs = " -s TOTAL_STACK=1MB " + emscriptenLinkerFlags;
        PlayerSettings.WebGL.compressionFormat = compressionFormat;

        var levels = new string[] { "Assets/2DGamekit/Scenes/Start.unity",
                                    "Assets/2DGamekit/Scenes/Zone1.unity",
                                    "Assets/2DGamekit/Scenes/Zone2.unity",
                                    "Assets/2DGamekit/Scenes/Zone3.unity",
                                    "Assets/2DGamekit/Scenes/Zone4.unity",
                                    "Assets/2DGamekit/Scenes/Zone5.unity"
                                  };
        Debug.Log("Starting a HTML5 build with Emscripten linker flags \"" + PlayerSettings.WebGL.emscriptenArgs + "\" to directory \"" + path + "\"...");

        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);
        buildLogFile = path + "/build_log.txt";

        WriteLog("Unity version: " + Application.unityVersion);
        WriteLog("Project: " + Application.companyName + " " + Application.productName + " " + Application.version);
        WriteLog("Build date: " + DateTime.Now.ToString("yyyy MMM dd HH:mm:ss"));
        WriteLog("");
        var buildStart = DateTime.Now;
        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(levels, path, BuildTarget.WebGL, path.Contains("development") ? BuildOptions.Development : BuildOptions.None);
        var buildEnd = DateTime.Now;
        Debug.Log("HTML5 build finished in " + TimeSpanToHHMMSSString(buildEnd.Subtract(buildStart)) + " to directory " + path);
        WriteLog("HTML5 build finished in " + TimeSpanToHHMMSSString(buildEnd.Subtract(buildStart)));
        WriteLog("");
        ulong totalSize = 0;
        foreach (var f in report.files)
        {
            string relativePath = PathRelativeTo(f.path, path);
            if (relativePath.StartsWith(".."))
                continue; // report.files contains paths that are not part of the HTML5/WebGL build output (such as "Temp/StagingArea/Data/Managed/System.Xml.Linq.dll"), so ignore all those
            Debug.Log(relativePath + ": " + FormatBytes(f.size));
            WriteLog(relativePath + ": " + FormatBytes(f.size));
            totalSize += f.size;
        }
        Debug.Log($"Total output size (Compression {compressionFormat.ToString()}): " + FormatBytes(totalSize));
        WriteLog("");
        WriteLog($"Total output size (Compression {compressionFormat.ToString()}): " + FormatBytes(totalSize));
    }

    static void DoHtml5Build(string kind, string emscriptenLinkerFlags, WebGLCompressionFormat compressionFormat)
    {
        var date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var path = System.IO.Path.GetFullPath("C:/code/html5_builds/" + Application.productName + "_" + date + "_" + kind);
//        var path = System.IO.Path.GetFullPath(Application.dataPath + "/../html5_builds/" + Application.productName + "_" + date + "_" + kind);
        DoHtml5BuildToDirectory(path, emscriptenLinkerFlags, compressionFormat);
    }

    static void DoHtml5BuildAskDirectory(string emscriptenLinkerFlags, WebGLCompressionFormat compressionFormat)
    {
        var path = EditorUtility.SaveFolderPanel("Choose Output Location for HTML5 Export", "", "");
        DoHtml5BuildToDirectory(path, emscriptenLinkerFlags, compressionFormat);
    }

    [MenuItem("HTML5 Export/Wasm+Development uncompressed...")]
    static void DoDevelopmentExportUncompressed()
    {
        DoHtml5Build("wasm_development_uncompressed", "", WebGLCompressionFormat.Disabled);
    }

    [MenuItem("HTML5 Export/Wasm+Release gzipped...")]
    static void DoReleaseExportGzipped()
    {
        DoHtml5Build("wasm_release_gzipped", "", WebGLCompressionFormat.Gzip);
    }

    [MenuItem("HTML5 Export/Wasm+Release uncompressed...")]
    static void DoReleaseExportUncompressed()
    {
        DoHtml5Build("wasm_release", "", WebGLCompressionFormat.Disabled);
    }

    [MenuItem("HTML5 Export/Wasm+Release+Profiling uncompressed...")]
    static void DoProfilingExport()
    {
        DoHtml5Build("wasm_release_profiling", "--profiling-funcs", WebGLCompressionFormat.Disabled);
    }

    [MenuItem("HTML5 Export/Wasm+Release+CpuProfiler uncompressed...")]
    static void DoCpuProfilerExport()
    {
        DoHtml5Build("wasm_release_cpuprofiler", "--profiling-funcs --cpuprofiler", WebGLCompressionFormat.Disabled);
    }

    [MenuItem("HTML5 Export/Wasm+Release+MemoryProfiler uncompressed...")]
    static void DoMemoryProfilerExport()
    {
        DoHtml5Build("wasm_release_memoryprofiler", "--profiling-funcs --memoryprofiler", WebGLCompressionFormat.Disabled);
    }
}
