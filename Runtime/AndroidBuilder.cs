#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace AndroidBuildSettings.Runtime
{
    public class AndroidBuilder
    {
        public static void Build(string locationPath, bool developmentBuild, string buildAndRun, string bundleVersion, int bundleVersionCode, Action HandleVersion, bool buildAppBundle, bool enableSigning, string keystorePassword, string aliasPassword)
        {
            List<string> scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if(scene.enabled)
                    scenes.Add(scene.path);
            }


            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = scenes.ToArray();
            buildPlayerOptions.locationPathName = locationPath;
            buildPlayerOptions.target = BuildTarget.Android;
            buildPlayerOptions.options = BuildOptions.None;
            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
            if (developmentBuild)
            {
                buildPlayerOptions.options = BuildOptions.Development;
            }
            
            PlayerSettings.bundleVersion = bundleVersion;
            PlayerSettings.Android.bundleVersionCode = bundleVersionCode;

            if (enableSigning)
            {
                PlayerSettings.keystorePass = keystorePassword;
                PlayerSettings.keyaliasPass = aliasPassword;
            }

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("BuildPlayerOptions\n"
                          + "Scenes: " + string.Join(",", buildPlayerOptions.scenes) + "\n"
                          + "Build location: " + buildPlayerOptions.locationPathName + "\n"
                          + "Options: " + buildPlayerOptions.options + "\n"
                          + "Target: " + buildPlayerOptions.target);
                Debug.Log("Build succeeded: " + summary.totalSize + " bytes");

                if (buildAndRun != "None")
                {
                    InstallApkFile(locationPath, buildAndRun);
                }
                
                HandleVersion?.Invoke();
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.Log("Build failed");
            }
        }
        
        static void p_DataReceived(object sender, DataReceivedEventArgs e)
        {
            // Manipulate received data here
            string MyPhones = e.Data.ToString();
            UnityEngine.Debug.Log(MyPhones);
        }

        public static void InstallApkFile(string locationPath, string buildAndRun)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
                
            psi.FileName = Path.Combine(EditorPrefs.GetString("AndroidSdkRoot"), "platform-tools/adb.exe");
            psi.Arguments = $"-s {buildAndRun.Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries)[1]} install -r {locationPath}";
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            Process p = new Process();
            p.StartInfo = psi;
            p.OutputDataReceived += p_DataReceived;
            p.EnableRaisingEvents = true;
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
        }
    }
}
#endif