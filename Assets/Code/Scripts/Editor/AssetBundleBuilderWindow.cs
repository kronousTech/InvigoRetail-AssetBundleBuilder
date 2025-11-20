using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InvigoRetail.AssetBundleBuilderEditor
{
    public class AssetBundleBuilderWindow : EditorWindow
    {
        private string m_outputPath = string.Empty;
        private BuildTarget m_buildTarget = BuildTarget.WebGL;
        private BuildAssetBundleOptions m_buildOption = BuildAssetBundleOptions.ForceRebuildAssetBundle;

        [MenuItem("InvigoRetail/AssetBundle Builder")]
        private static void OpenWindow()
        {
            GetWindow<AssetBundleBuilderWindow>("AssetBundle Builder");
        }

        private void OnGUI()
        {
            GUILayout.Label("InvigoRetail AssetBundle Builder", EditorStyles.centeredGreyMiniLabel);
            
            m_buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", m_buildTarget);
            
            m_buildOption = (BuildAssetBundleOptions)EditorGUILayout.EnumPopup("Build Options", m_buildOption);
            
            EditorGUILayout.BeginHorizontal();
            m_outputPath = EditorGUILayout.TextField(m_outputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Output Folder", "", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    m_outputPath = selected + "/";
                }
            }
            if (GUILayout.Button("Open", GUILayout.Width(45)))
            {
                EditorUtility.RevealInFinder(m_outputPath);
            }
            EditorGUILayout.EndVertical();

            if (!Directory.Exists(m_outputPath))
            {
                EditorGUILayout.HelpBox("The path doesn't exist", MessageType.Error);
            }
            else if(GUILayout.Button(nameof(BuildAssetBundles)))
            {
                BuildAssetBundles();
            }
            else if (GUILayout.Button(nameof(AddDependencies)))
            {
                AddDependencies();
            }
        }

        private void AddDependencies()
        {
            UnityEngine.Object asset;
            string dependencyPath;
            AssetImporter importer;

            foreach (var bundleName in AssetDatabase.GetAllAssetBundleNames())
            {
                foreach (var assetPath in AssetDatabase.GetAssetPathsFromAssetBundle(bundleName))
                {
                    asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

                    foreach (var dependency in EditorUtility.CollectDependencies((new UnityEngine.Object[] { asset })))
                    {
                        dependencyPath = AssetDatabase.GetAssetPath(dependency);
                        if (string.IsNullOrEmpty(dependencyPath) || dependencyPath.StartsWith("Library"))
                        {
                            continue;
                        }

                        importer = AssetImporter.GetAtPath(dependencyPath);
                        if (importer != null)
                        {
                            importer.assetBundleName = bundleName;

                            Debug.Log("Assigned to bundle: " + dependency.name + " (" + dependencyPath + ")");
                        }
                    }
                }
            }
        }

        private void BuildAssetBundles()
        {
            var baseManifest = BuildPipeline.BuildAssetBundles(m_outputPath, m_buildOption, m_buildTarget);
            if (baseManifest == null)
            {
                Debug.LogError($"{nameof(AssetBundleBuilderWindow)}:" +
                    $"Base manifest came as null.");

                return;
            }
            
            var bundles = baseManifest.GetAllAssetBundles();

            // Add version to manifest
            foreach (var item in bundles)
            {
                var manifestPath = m_outputPath + item + ".manifest";
                var reader = new StreamReader(manifestPath);
                var manifestText = reader.ReadToEnd();

                if (manifestText.Contains("BundleVersion: "))
                {
                    var index = manifestText.IndexOf(System.Environment.NewLine);
                    manifestText = manifestText.Substring(index + System.Environment.NewLine.Length);
                }
                reader.Close();

                var writer = new StreamWriter(manifestPath, false);
                writer.WriteLine($"BundleVersion: {GetCurrentVersion()}\n{manifestText}");
                writer.Close();
            }
        }
        private static string GetCurrentVersion()
        {
            return DateTime.Now.ToString("yyyyMMddmmss");
        }
    }
}

