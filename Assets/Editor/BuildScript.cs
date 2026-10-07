using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ZombieApocalypse.Editor
{
    public static class BuildScript
    {
        [MenuItem("Build/Build Windows 64-Bit Release")]
        public static void PerformBuild()
        {
            string outputDirectory = @"D:\UnityProjects\ZombieApocalypseSurvival_Builds\Windows64";
            string outputPath = Path.Combine(outputDirectory, "ZombieApocalypseSurvival.exe");

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string[] scenes = new string[]
            {
                "Assets/Scenes/MainMenu/MainMenu.unity",
                "Assets/Scenes/Gameplay/Gameplay.unity",
                "Assets/Scenes/Test/TestArena.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"[BuildScript] Starting Windows 64-bit production build to: {outputPath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] BUILD SUCCESSFUL! Size: {summary.totalSize} bytes. Duration: {summary.totalTime}");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[BuildScript] BUILD FAILED! Errors: {summary.totalErrors}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}
