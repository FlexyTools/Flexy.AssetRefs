namespace Flexy.AssetRefs.LoadExtensions;

public static class LoadExts
{
   	public static	T?				LoadAssetSync<T>	( this AssetRef<T> @ref ) where T : Object		=> AssetRef.AssetsLoader.LoadAssetSync<T>( @ref );
    public static	UniTask<T?>		LoadAssetAsync<T>	( this AssetRef<T> @ref ) where T : Object		=> AssetRef.AssetsLoader.LoadAssetAsync<T>( @ref );

    // Scene loading have GameObject context parameter - it is used internally to know where scene loading was called from.
    // You just need to pass gameObject of MonoBehaviour. i.e. this.gameObject  
	public static	LoadSceneTask	LoadSceneAsync		( this SceneRef @ref, GameObject context, LoadSceneParameters p )		=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, new(p.loadSceneMode, p.localPhysicsMode), context );
	public static	LoadSceneTask	LoadSceneAsync		( this SceneRef @ref, GameObject context, LoadSceneTask.Parameters p )	=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, p, context );
	public static	LoadSceneTask	LoadSceneAsync		( this SceneRef @ref, GameObject context, LoadSceneMode loadMode = LoadSceneMode.Additive )		=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, new (loadMode), context );
}