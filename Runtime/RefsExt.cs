namespace Flexy.AssetRefs.LoadExtensions;

public static class RefsExt
{
   	public static	T?				LoadAssetSync<T>	( this AssetRef<T> @ref ) where T : Object		=> AssetRef.AssetsLoader.LoadAssetSync<T>( @ref );
    public static	UniTask<T?>		LoadAssetAsync<T>	( this AssetRef<T> @ref ) where T : Object		=> AssetRef.AssetsLoader.LoadAssetAsync<T>( @ref );

    public static	String?			GetSceneName		( this SceneRef @ref )													=> AssetRef.AssetsLoader.GetSceneName( @ref );
	public static	LoadSceneTask	LoadSceneAsync		( this SceneRef @ref, GameObject context, LoadSceneParameters p )		=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, new(p.loadSceneMode, p.localPhysicsMode), context );
	public static	LoadSceneTask	LoadSceneAsync		( this SceneRef @ref, GameObject context, LoadSceneTask.Parameters p )	=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, p, context );
	public static	LoadSceneTask	LoadSceneAsync		( this SceneRef @ref, GameObject context, LoadSceneMode loadMode = LoadSceneMode.Additive )		=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, new (loadMode), context );
}