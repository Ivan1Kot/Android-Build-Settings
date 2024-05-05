#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AndroidBuildSettings.Runtime
{

    public class AndroidBuildSettingEditorWindow: EditorWindow
    {
        [SerializeField] public int index = 0;
        [SerializeField] public string buildFilename = "MyApplication";
        [SerializeField] public bool developmentBuild;
        [SerializeField] public bool appBundleBuild;
        [SerializeField] public string outputFileDirectory = "C:/UnityBuilds";
        
        [SerializeField] List<string> devicesOptions = new List<string>() {"None"};
        [SerializeField] List<string> Listas = new List<string>();
        [SerializeField] public static string MyPhones;

        [SerializeField] public int version1, version2, version3, bundleVersion = 0;
        [SerializeField] public bool appVersionAutoincrement, bundleVersionAutoincrement = true;
        [SerializeField] public bool deletePreviousBuild, openAfterBuild = true;
        [SerializeField] public string previousBuildPath;
        
        [SerializeField] public bool enableSigning;
        [SerializeField] public string keystorePassword;
        [SerializeField] public string aliasPassword;

        private static AndroidBuildSettingEditorWindow wnd;

        [MenuItem("Window/Android Build Settings")]
        public static void ShowExample()
        {
            wnd = GetWindow<AndroidBuildSettingEditorWindow>(false, "Android Build Settings", true);
            wnd.minSize = new Vector2(450, 400);
        }
        
        private void OnInspectorUpdate()
        {
            Repaint();
        }
        
        protected void OnEnable ()
        {
            // Here we retrieve the data if it exists or we save the default field initialisers we set above
            var data = EditorPrefs.GetString(Application.productName+".AndroidBuildSettingEditorWindow", JsonUtility.ToJson(this, false));
            // Then we apply them to this window
            JsonUtility.FromJsonOverwrite(data, this);
        }

        private void OnDisable()
        {
            // We get the Json data
            var data = JsonUtility.ToJson(this, false);
            // And we save it
            EditorPrefs.SetString(Application.productName+".AndroidBuildSettingEditorWindow", data);
        }

        public string GetVersion()
        {
            return version1.ToString()+ "."+ version2.ToString()+ "."+ version3.ToString();
        }
        
        public string GetBuildLocation()
        {
            return Path.Combine(outputFileDirectory, buildFilename + '-' + GetVersion() + ".apk");
        }

        public void OnGUI()
        {
            int uniformPadding = 10;
            RectOffset padding = new RectOffset(uniformPadding, uniformPadding, uniformPadding, uniformPadding);
//Builds Layout Area from padding values
            Rect area = new Rect(padding.right, padding.top, position.width - (padding.right + padding.left), position.height - (padding.top + padding.bottom));
 
            GUILayout.BeginArea(area);
// Your GUI Code

            //GUILayout.FlexibleSpace();
            GUILayout.Space(15);
            
            EditorGUILayout.BeginHorizontal();
            Texture2D LogoTex = (Texture2D)Resources.Load("android"); //don't put png
            GUILayout.Label(LogoTex, EditorStyles.iconButton);

            GUILayout.Label("Android", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            
            EditorGUILayout.BeginHorizontal();
            buildFilename = EditorGUILayout.TextField("Build file name", buildFilename);
            GUILayout.Label('-'+GetVersion()+".apk");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            developmentBuild = EditorGUILayout.Toggle("Development build", developmentBuild);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            appBundleBuild = EditorGUILayout.Toggle("Build App Bundle (Google Play)", appBundleBuild);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Output file directory", outputFileDirectory);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                outputFileDirectory = EditorUtility.OpenFolderPanel("Select output directory", "", "");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            index = EditorGUILayout.Popup("Run device", index, devicesOptions.ToArray());
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                devicesOptions = new List<string>() {"None"};
                Listas = new List<string>();
                index = 0;
                
                ProcessStartInfo psi = new ProcessStartInfo();
                
                psi.FileName = Path.Combine(EditorPrefs.GetString("AndroidSdkRoot"), "platform-tools/adb.exe");
                psi.Arguments = "devices -l";
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
                
                foreach (var x in Listas)
                {
                    devicesOptions.Add(x);
                }
                
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            version1 = EditorGUILayout.IntField("App Version", version1);
            GUILayout.Label(".");
            version2 = EditorGUILayout.IntField(version2);
            GUILayout.Label(".");
            version3 = EditorGUILayout.IntField(version3);
            GUILayout.Space(15);
            appVersionAutoincrement = GUILayout.Toggle(appVersionAutoincrement, "Autoincrement");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            bundleVersion = EditorGUILayout.IntField("Bundle Version", bundleVersion);
            GUILayout.Space(15);
            bundleVersionAutoincrement = GUILayout.Toggle(bundleVersionAutoincrement, "Autoincrement");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Signing", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            enableSigning = EditorGUILayout.Toggle("Enable signing", enableSigning);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            keystorePassword = EditorGUILayout.TextField("Keystore password", keystorePassword);
            GUILayout.Label("");
            GUILayout.Label("");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            aliasPassword = EditorGUILayout.TextField("Alias password", aliasPassword);
            GUILayout.Label("");
            GUILayout.Label("");
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            
            EditorGUILayout.BeginHorizontal();
            deletePreviousBuild = EditorGUILayout.Toggle("Delete Previous Build", deletePreviousBuild);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            openAfterBuild = EditorGUILayout.Toggle("Open Build Folder After Build", openAfterBuild);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            //GUILayout.Space(60);
            
            GUILayout.Label("Current build:", EditorStyles.miniLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(previousBuildPath, MessageType.None);
            if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(45)))
            {
                EditorUtility.RevealInFinder(GetBuildLocation());
            }

            if (GUILayout.Button("Install", EditorStyles.miniButton, GUILayout.Width(45)))
            {
                if (devicesOptions[index] != "None")
                {
                    AndroidBuilder.InstallApkFile(previousBuildPath, devicesOptions[index]);
                }
                else
                {
                    UnityEngine.Debug.LogError("Select android install device!");
                }
            }

            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            
            GUILayout.Label("The output file path after build:", EditorStyles.miniLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(GetBuildLocation(), MessageType.None);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Player Settings..."))
            {
                SettingsService.OpenProjectSettings("Project/Player");
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Build", GUILayout.Width(100)))
            {
                AndroidBuilder.Build(GetBuildLocation(), developmentBuild, "None", GetVersion(), bundleVersion, HandleVersion, appBundleBuild, enableSigning, keystorePassword, aliasPassword);
            }
            if (GUILayout.Button("Build And Run", GUILayout.Width(100)))
            {
                AndroidBuilder.Build(GetBuildLocation(), developmentBuild, devicesOptions[index], GetVersion(), bundleVersion, HandleVersion, appBundleBuild, enableSigning, keystorePassword, aliasPassword);
            }
            EditorGUILayout.EndHorizontal();
            
            
            GUILayout.EndArea();
        }
        
        void p_DataReceived(object sender, DataReceivedEventArgs e)
        {
            // Manipulate received data here
            MyPhones = e.Data.ToString();
            // UnityEngine.Debug.Log(MyPhones);
            if (e.Data.Contains("device") && !e.Data.Contains("List"))
            {
                int index = MyPhones.LastIndexOf("model:") + 6;
                if (index > 0)
                {
                    string serialNumber = MyPhones.Split(' ')[0];
                    MyPhones = MyPhones.Substring(index);
                    MyPhones = MyPhones.Substring(0, MyPhones.IndexOf("device"));
                    Listas.Add(MyPhones+ $"({serialNumber})");
                }
            }

            // if no devices, then there will be only "List of devices attached: "
        }

        public void HandleVersion()
        {
            if (previousBuildPath != null && deletePreviousBuild && File.Exists(previousBuildPath))
            {
                File.Delete(previousBuildPath);
            }

            if (openAfterBuild)
            {
                EditorUtility.RevealInFinder(GetBuildLocation());
            }
            previousBuildPath = GetBuildLocation();
            
            if (appVersionAutoincrement)
            {
                version3++;
            }

            if (bundleVersionAutoincrement)
            {
                bundleVersion++;
            }
            
            // We get the Json data
            var data = JsonUtility.ToJson(this, false);
            // And we save it
            EditorPrefs.SetString("AndroidBuildSettingEditorWindow", data);
        }
    }
    
    
}
#endif