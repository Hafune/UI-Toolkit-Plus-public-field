private static void OnPostprocessAllAssets(
    string[] importedAssets,
    string[] deletedAssets,
    string[] movedAssets,
    string[] movedFromAssetPaths)
{
    foreach (var uxmlPath in importedAssets.Where(p => p.EndsWith(".uxml")))
    {
        if (ShouldGenerateCsFile(uxmlPath))
            GenCsClassGenerator.GenerateGenCs(uxmlPath, false);
    }

    if (importedAssets.Any(p => p.EndsWith(".uss")))
    {
        var uxmlPaths = AssetDatabase.FindAssets("t:VisualTreeAsset")
            .Select(AssetDatabase.GUIDToAssetPath);

        foreach (var uxmlPath in uxmlPaths)
        {
            if (ShouldGenerateCsFile(uxmlPath))
                GenCsClassGenerator.GenerateGenCs(uxmlPath, false);
        }
    }
}