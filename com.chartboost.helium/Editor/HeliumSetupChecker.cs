using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using ImportOptions = UnityEditor.PackageManager.UI.Sample.ImportOptions;

namespace Editor
{
    public class HeliumSetupChecker
    {
        private const string UnityAds = "UnityAds";
        private const string Helium = "Helium";
        private const string HeliumWindowTitle = "Helium Unity SDK - Integration Status Checker";
        private const string HeliumPackageName = "com.chartboost.helium";
        private const string HeliumSamplesInAssets = "Assets/Samples/Helium SDK";
        private const string UnityAdsPackageName = "com.unity.ads";
        private static readonly string HeliumSamplesMetaInAssets = $"{HeliumSamplesInAssets}.meta";
        private static readonly Version HeliumUnityAdsSupportedVersion = new Version(4, 2, 1);


        /// <summary>
        /// Finds a package in the Unity project non-restricted to the Unity Registry. Any package on the package.json file can be loaded with this method.
        /// </summary>
        /// <param name="packageName">Name of the package to fetch</param>
        /// <returns>PackageInfo of the found package, null if not found</returns>
        public static PackageInfo FindPackage(string packageName)
        {
            var packageJsons = AssetDatabase.FindAssets("package")
                .Select(AssetDatabase.GUIDToAssetPath).Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                .Select(PackageInfo.FindForAssetPath).ToList();

            return packageJsons.Find(x => x.name == packageName);
        }

        /// <summary>
        /// Imports a sample in the Helium Unity SDK package
        /// </summary>
        /// <param name="sampleName">Sample to include into project</param>
        /// <param name="version">Helium package version to use, must coincide with the currently installed version.</param>
        /// <returns>Import success status</returns>
        public static bool ImportSample(string sampleName, string version)
        {
            var sample = Sample.FindByPackage(HeliumPackageName, version).Single(x => x.displayName.Equals(sampleName));
            return sample.Import(ImportOptions.HideImportWindow | ImportOptions.OverridePreviousImports);
        }

        /// <summary>
        /// Re-imports a series of Samples based of a collection of Samples names. This is to only update what it's currently in place regardless of the version.
        /// </summary>
        /// <param name="existingSamples">Existing Samples to re-import regardless of the version. Name based</param>
        /// <param name="version">Helium package version to use, must coincide with the currently installed version.</param>
        public static void ReimportExistingHeliumSamples(ICollection<string> existingSamples, string version)
        {
            Directory.Delete(HeliumSamplesInAssets, true);
            File.Delete(HeliumSamplesMetaInAssets);
            AssetDatabase.Refresh();

            var allSamples = Sample.FindByPackage(HeliumPackageName, version);

            foreach (var sample in allSamples)
            {
                if (existingSamples.Contains(sample.displayName))
                    sample.Import(ImportOptions.HideImportWindow | ImportOptions.OverridePreviousImports);
            }
        }

        /// <summary>
        /// Used to attempt to update all existing Ad Adapters without extra input. Good for CI/CD usage and update of Adapters.
        /// </summary>
        /// <returns>Update attempt status</returns>
        public static bool ReimportExistingAdapters()
        {
            var helium = FindPackage(HeliumPackageName);

            if (!Directory.Exists(HeliumSamplesInAssets))
                return false;

            var subdirectories = Directory.GetDirectories(HeliumSamplesInAssets);
            if (subdirectories.Length <= 0)
                return false;

            var versionDirectory = subdirectories[0];
            var importedDependencies = new HashSet<string>();
            // find all samples/ad adapters
            foreach (var imported in Directory.GetDirectories(versionDirectory))
            {
                var sampleName = Path.GetFileName(imported);
                importedDependencies.Add(sampleName);
            }

            ReimportExistingHeliumSamples(importedDependencies, helium.version);
            return true;
        }

        public static bool CheckUnityAdsIntegration(string heliumVersion = null)
        {
            if (string.IsNullOrEmpty(heliumVersion))
            {
                var helium = FindPackage(HeliumPackageName);
                if (!Version.TryParse(helium.version, out var heliumFoundVersion))
                {
                    Debug.LogError($"Failed to parse Helium Unity SDK version: {helium.version}");
                    return false;
                }
                heliumVersion = heliumFoundVersion.ToString();
            }
            
            var unityAdsDependencyPath = $"Assets/Samples/Helium SDK/{heliumVersion}/UnityAds/Editor/Optional-HeliumUnityAdsDependencies.xml";

            // check if UnityAds is integrated
            if (!File.Exists(unityAdsDependencyPath))
                return false;
            
            var unityAdsPackage = FindPackage(UnityAdsPackageName);
            
            if (unityAdsPackage != null)
            {
                if (!Version.TryParse(unityAdsPackage.version, out var unityAdsVersion))
                    return false;

                if (!unityAdsVersion.Equals(HeliumUnityAdsSupportedVersion))
                {
                    EditorUtility.DisplayDialog(
                        HeliumWindowTitle,
                        $"UnityAds SDK integrated through Unity Package Manager with version: {unityAdsPackage.version}. Helium recommended version is {HeliumUnityAdsSupportedVersion}.\n\nUnexpected behaviors can occur.",
                        "Ok");
                }
                return true;
            }
            
            var unityAdsDependencyLines = File.ReadLines(unityAdsDependencyPath).ToList();
            var unityAdsSDKCommented = $"<!-- <androidPackage spec=\"com.unity3d.ads:unity-ads:{HeliumUnityAdsSupportedVersion}\"/> -->";
            var commentedLineIndex = unityAdsDependencyLines.FindIndex(line => line.Contains(unityAdsSDKCommented));

            if (commentedLineIndex == -1) 
                return true;
            
            var updateUnityAdsSample = EditorUtility.DisplayDialog(
                HeliumWindowTitle,
                "Helium UnityAds Samples/Dependency found, but UnityAdsSDK is commented. This will lead to a non-functional adapter.\n\nDo you wish to uncomment it?",
                "Yes", "No", DialogOptOutDecisionType.ForThisMachine, "unity-ads");

            if (!updateUnityAdsSample)
                return false;

            var unityAdsSDKUncommented = $"        <androidPackage spec=\"com.unity3d.ads:unity-ads:{HeliumUnityAdsSupportedVersion}\"/>";
            unityAdsDependencyLines[commentedLineIndex] = unityAdsSDKUncommented;
            File.WriteAllLines(unityAdsDependencyPath, unityAdsDependencyLines);
            return true;
        }

        [MenuItem("Helium/Integration/UnityAds Check", false, 1)]
        public static void CheckUnityAdsIntegrationEditor()
        {
            CheckUnityAdsIntegration();
        }

        /// <summary>
        /// Used to update all existing adapters by Devs choice. This will utilize current's Helium Package version to override all adapters with such version.
        /// </summary>
        [MenuItem("Helium/Integration/Force Reimport Adapters", false, 2)]
        public static void ForceReimportExistingAdapters()
        {
            var confirmUpdate = EditorUtility.DisplayDialog(HeliumWindowTitle,
                "Attempting to force reimport all existing adapters.\n\nIs this intentional?", "Yes", "No");

            if (confirmUpdate)
                ReimportExistingAdapters();
        }

        /// <summary>
        /// Used to detect and address general Helium Integration issues
        /// </summary>
        [MenuItem("Helium/Integration/Status Check", false, 0)]
        public static void CheckHeliumIntegration()
        {
            var helium = FindPackage(HeliumPackageName);

            // check if Helium Samples exists
            if (Directory.Exists(HeliumSamplesInAssets))
            {
                var subDirectories = Directory.GetDirectories(HeliumSamplesInAssets);

                // no versioning folder
                if (subDirectories.Length <= 0)
                {
                    var addHeliumSample = EditorUtility.DisplayDialog(
                        HeliumWindowTitle,
                        "Helium Samples directory found, but not ad adapters in place.\n\nMake sure to include at least the Helium dependencies.\n\nWould you like to add them?",
                        "Yes", "No");

                    if (addHeliumSample)
                        ImportSample(Helium, helium.version);
                }
                // at least one versioning sample
                else
                {
                    // we have found a directory with dependencies
                    var versionDirectory = subDirectories[0];

                    // get the version of the dependencies found
                    var heliumVersionStr = Path.GetFileName(versionDirectory);

                    // parse versioning folder vesion
                    if (!Version.TryParse(heliumVersionStr, out var versionInAssets))
                    {
                        EditorUtility.DisplayDialog(
                            HeliumWindowTitle,
                            $"Failed to parse version {heliumVersionStr} in Assets, please contact Helium Support.",
                            "Ok");
                        return;
                    }

                    var importedDependencies = new HashSet<string>();

                    // find all samples/ad adapters
                    foreach (var imported in Directory.GetDirectories(versionDirectory))
                    {
                        var sampleName = Path.GetFileName(imported);
                        importedDependencies.Add(sampleName);
                    }

                    // no samples/ad adapters
                    if (importedDependencies.Count <= 0)
                    {
                        var addHeliumSamples = EditorUtility.DisplayDialog(
                            HeliumWindowTitle,
                            $"Helium Samples/Dependencies directory found for version {versionInAssets}, but not Ad adapters in place.\n\nYou must at least include the Helium dependencies.\n\nWould you like to add them?",
                            "Yes", "No");

                        if (addHeliumSamples)
                            ImportSample(Helium, helium.version);
                    }
                    // at least one sample
                    else
                    {
                        if (!importedDependencies.Contains(Helium))
                        {
                            var addHeliumSamples = EditorUtility.DisplayDialog(
                                HeliumWindowTitle,
                                $"Helium Samples/Dependencies not found in Assets.\n\nWould you like to add them?",
                                "Yes", "No");

                            if (addHeliumSamples)
                                ImportSample(Helium, helium.version);
                        }
                    }

                    // parse package version
                    if (!Version.TryParse(helium.version, out var versionInPackage))
                    {
                        EditorUtility.DisplayDialog(
                            HeliumWindowTitle,
                            $"Failed to parse version {heliumVersionStr} in Package, please contact Helium Support.",
                            "Ok");
                    }

                    // act based off version
                    if (versionInAssets < versionInPackage)
                    {
                        var dialogInput = EditorUtility.DisplayDialog(
                            HeliumWindowTitle,
                            $"Newer Samples/Dependencies for version {versionInAssets}, package is using version {versionInPackage}.\n\nDo you wish to update your existing Ad adapters?",
                            "Yes", "No");

                        if (dialogInput)
                            ReimportExistingHeliumSamples(importedDependencies, helium.version);
                    }
                    else if (versionInAssets > versionInPackage)
                    {
                        var dialogInput = EditorUtility.DisplayDialog(
                            HeliumWindowTitle,
                            $"Older Samples/Dependencies for version {versionInAssets} found, package is using newer version {versionInPackage}.\n\nDo you wish to downgrade your existing Ad adapters?\n\n**This is probably a bad setup, contact Helium Support**",
                            "Yes", "No");

                        if (dialogInput)
                            ReimportExistingHeliumSamples(importedDependencies, helium.version);
                    }
                    
                    // check for Unity Ads integration
                    if (importedDependencies.Contains(UnityAds))
                    {
                        CheckUnityAdsIntegration(helium.version);
                    }
                    
                    AssetDatabase.Refresh();
                }
            }
            // no samples at all!
            else
            {
                var addHeliumSample = EditorUtility.DisplayDialog(
                    HeliumWindowTitle,
                    "No Samples directory found.\n\nMake sure to include at least the Helium dependencies.\n\nWould you like to add them?",
                    "Yes", "No");

                if (addHeliumSample)
                    ImportSample(Helium, helium.version);
            }
        }
    }
}