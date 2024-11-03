namespace Flexy.AssetRefs;

public static class RefsExt
{
   	public static			T					LoadAssetSync<T>		( this AssetRef<T> @ref, Boolean throwException = false ) where T : Object	=> AssetRef.AssetsLoader.LoadAssetSync<T>( @ref, throwException );
    public static			UniTask<T> 			LoadAssetAsync<T>		( this AssetRef<T> @ref, Boolean throwException = false ) where T : Object	=> AssetRef.AssetsLoader.LoadAssetAsync<T>( @ref, throwException );

    public static			String				GetSceneName			( this SceneRef @ref )																			=> AssetRef.AssetsLoader.GetSceneName( @ref );
	public static			SceneTask			LoadSceneAsync			( this SceneRef @ref, GameObject context, LoadSceneMode loadMode = LoadSceneMode.Additive )		=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, new (loadMode), context );
	public static			SceneTask			LoadSceneAsync			( this SceneRef @ref, GameObject context, LoadSceneParameters p )								=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, new(p.loadSceneMode, p.localPhysicsMode), context );
	public static			SceneTask			LoadSceneAsync			( this SceneRef @ref, GameObject context, SceneTask.Parameters p )								=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, p, context );
	
	public static			Int64				GetDownloadBytes		( this AssetRef @ref )		=> AssetRef.AssetsLoader.Package_GetDownloadBytes( @ref );
	public static			LoadTask<Boolean>	DownloadAsync			( this AssetRef @ref )		=> AssetRef.AssetsLoader.Package_DownloadAsync( @ref );
}