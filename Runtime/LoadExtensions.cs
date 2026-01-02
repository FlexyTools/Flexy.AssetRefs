namespace Flexy.AssetRefs.LoadExtensions;

public static class LoadExts
{
   	public static	T?				LoadAssetSync<T>	( this AssetRef<T> @ref ) where T : Object		=> AssetRef.AssetLoader.LoadAssetSync<T>( @ref );
    public static	UniTask<T?>		LoadAssetAsync<T>	( this AssetRef<T> @ref ) where T : Object		=> AssetRef.AssetLoader.LoadAssetAsync<T>( @ref );

    // Scene loading have GameObject context parameter - it is used internally to know where scene loading was called from.
    // You just need to pass gameObject of MonoBehaviour. i.e. this.gameObject  
	public static	LoadSceneTask	LoadSceneAsync		( this SceneRef @ref, GameObject context, LoadSceneParameters p )		=> SceneRef.SceneLoader.LoadSceneAsync( @ref, new(p.loadSceneMode, p.localPhysicsMode), context );
	public static	LoadSceneTask	LoadSceneAsync		( this SceneRef @ref, GameObject context, LoadSceneTask.Parameters p )	=> SceneRef.SceneLoader.LoadSceneAsync( @ref, p, context );
	public static	LoadSceneTask	LoadSceneAsync		( this SceneRef @ref, GameObject context, LoadSceneMode loadMode = LoadSceneMode.Additive )		=> SceneRef.SceneLoader.LoadSceneAsync( @ref, new (loadMode), context );
}