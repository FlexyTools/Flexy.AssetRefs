namespace Flexy.AssetRefs.AssetLoaders;

public class AssetLoader_ContentDirectory : AssetLoader
{
	public AssetLoader_ContentDirectory( ContentService service ) => _service = service;

	private readonly ContentService _service;

	protected override UniTask<T?>	LoadAssetAsync_Impl<T>	( AssetRef @ref )	where T : class	=> _service.LoadContentAssetAsync<T>(@ref);
	protected override T?			LoadAssetSync_Impl<T>	( AssetRef @ref )	where T : class	=> _service.LoadContentAssetSync<T>	(@ref);
}