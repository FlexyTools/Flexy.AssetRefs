#if UNITY_6000_6_OR_NEWER
using Unity.Loading;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using BuildCompression = UnityEngine.BuildCompression;

namespace Flexy.AssetRefs.Editor.PipelineTasks;

[Serializable]
public class BuildContentDirectory : IPipelineTask
{
	[SerializeField] ContentService_LocalContentDirectory	ServicePrefab	= null!;
	[SerializeField] String					OutputPath		= "Assets/StreamingAssets/MainCD";
	[SerializeField] String					BuildName		= "GameContent";
	[SerializeField] CompressionType		Compression		= default;
	[SerializeField] BuildContentOptions	Options			= BuildContentOptions.FailBuildWhenErrorsLogged;
	[SerializeField] Boolean				LeaveFirstSceneForBuildSettings = true;
	[SerializeField] Boolean				CleanupContentCatalog			= true;

	const String CatalogAssetPath		= "Assets/ContentCatalog.asset";

	public void Run( Pipeline ppln, Context ctx )
	{
		ContentService.EditorSetupContentServiceResourceRef(ServicePrefab);

		if (String.IsNullOrWhiteSpace(OutputPath))
			throw new BuildFailedException($"{nameof(OutputPath)} can not be empty.");

		if (String.IsNullOrWhiteSpace(BuildName))
			throw new BuildFailedException($"{nameof(BuildName)} can not be empty.");

		if (String.IsNullOrWhiteSpace(CatalogAssetPath) || !CatalogAssetPath.StartsWith("Assets/", StringComparison.Ordinal))
			throw new BuildFailedException($"{nameof(CatalogAssetPath)} must be an asset path below 'Assets/'.");

		var assetRefs		= new List<AssetRef>();
		var assetLoadables	= new List<LoadableObjectId>();
		var sceneLoadables	= new List<LoadableSceneId>();
		var sceneNames		= new List<String>();

		foreach (var @ref in ctx.Get<RefsList>().Where(@ref => !@ref.IsNone).Distinct())
		{
			var asset = AssetLoader.EditorLoadAssetRaw(@ref);
			if (!asset)
			{
				Debug.LogError($"[ContentCatalogBuilder] Asset '{@ref}' is absent in '{ppln.name}' collector. Skipped.", ppln);
				continue;
			}

			if (asset is SceneAsset)
				continue; // It is already in scene list

			var objectId = LoadableObjectIdEditorUtility.CreateLoadableObjectId(asset);
			if (!objectId.IsValid)
				throw new BuildFailedException($"Can not create LoadableObjectId for '{@ref}' ({asset.name}).");

			assetRefs		.Add(@ref);
			assetLoadables	.Add(objectId);
		}

		var sceneList = ctx.Get<SceneList>().Where(@ref => !@ref.IsNone).Distinct().ToList();

		if (LeaveFirstSceneForBuildSettings)
		{
			SceneList.Internal.ReplaceRefs( ctx.Get<SceneList>(), new List<SceneRef>{sceneList[0]} );
			sceneList.RemoveAt(0);
		}

		foreach (var sceneRef in sceneList)
		{
			var sceneAsset = AssetLoader.EditorLoadAssetRaw(sceneRef.Raw) as SceneAsset;
			if (!sceneAsset)
				throw new BuildFailedException($"SceneRef '{sceneRef}' does not point to a scene asset.");

			sceneLoadables	.Add(LoadableSceneIdEditorUtility.CreateLoadableSceneId(sceneRef.Uid.ToGUID()));
			sceneNames		.Add(sceneAsset.name);
		}

		var directory = Path.GetDirectoryName(CatalogAssetPath);
		if (!String.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		var catalog = AssetDatabase.LoadAssetAtPath<ContentDirectoryCatalog>(CatalogAssetPath);
		if (!catalog)
		{
			if (AssetDatabase.LoadMainAssetAtPath(CatalogAssetPath))
				throw new BuildFailedException($"Asset '{CatalogAssetPath}' exists but is not a {nameof(ContentDirectoryCatalog)}.");

			catalog = ScriptableObject.CreateInstance<ContentDirectoryCatalog>();
			AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
		}

		catalog			.SetLoadables	(assetRefs.ToArray(), assetLoadables.ToArray(), sceneLoadables.ToArray(), sceneNames.ToArray());
		EditorUtility	.SetDirty		(catalog);
		AssetDatabase	.SaveAssets		();
		AssetDatabase	.ImportAsset	(CatalogAssetPath, ImportAssetOptions.ForceSynchronousImport);

		Directory.CreateDirectory(OutputPath);

		var parameters = new BuildContentDirectoryParameters
		{
			name			= BuildName,
			outputPath		= OutputPath,
			rootAssetPaths	= new[] { CatalogAssetPath },
			options			= Options,
			compression		= Compression switch
			{
				CompressionType.Lz4		=> BuildCompression.LZ4Runtime,
				CompressionType.Lz4HC	=> BuildCompression.LZ4,
				CompressionType.Lzma	=> BuildCompression.LZMA,
				_						=> default
			}
		};

		var report = BuildPipeline.BuildContentDirectory(parameters);
		
		if (CleanupContentCatalog)
			AssetDatabase.DeleteAsset(CatalogAssetPath);
		
		if (report.summary.result != BuildResult.Succeeded)
			throw new BuildFailedException($"Content directory build failed: {report.summary.result}.");

		Debug.Log(
			$"[ContentCatalogBuilder] Built content directory '{BuildName}' at '{Path.GetFullPath(OutputPath)}'. " +
			$"Assets: {assetRefs.Count}, scenes: {sceneLoadables.Count}, size: {report.summary.totalSize} bytes.");
	}
}
#endif