namespace Flexy.AssetRefs;

public static class RefsExt
{
   	public static			T?					LoadAssetSync<T>		( this AssetRef<T> @ref ) where T : Object														=> AssetRef.AssetsLoader.LoadAssetSync<T>( @ref );
    public static			UniTask<T?>			LoadAssetAsync<T>		( this AssetRef<T> @ref ) where T : Object														=> AssetRef.AssetsLoader.LoadAssetAsync<T>( @ref );

    public static			String?				GetSceneName			( this SceneRef @ref )																			=> AssetRef.AssetsLoader.GetSceneName( @ref );
	public static			SceneTask			LoadSceneAsync			( this SceneRef @ref, GameObject context, LoadSceneMode loadMode = LoadSceneMode.Additive )		=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, new (loadMode), context );
	public static			SceneTask			LoadSceneAsync			( this SceneRef @ref, GameObject context, LoadSceneParameters p )								=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, new(p.loadSceneMode, p.localPhysicsMode), context );
	public static			SceneTask			LoadSceneAsync			( this SceneRef @ref, GameObject context, SceneTask.Parameters p )								=> AssetRef.AssetsLoader.LoadSceneAsync( @ref, p, context );
	
	public static			T?					LoadAssetTypedSync<T>	( this AssetRef @ref ) where T : Object	=> new AssetRef<T>( @ref.Uid, @ref.SubId ).LoadAssetSync( );
	public static			UniTask<T?>			LoadAssetTyped<T>		( this AssetRef @ref ) where T : Object	=> new AssetRef<T>( @ref.Uid, @ref.SubId ).LoadAssetAsync( );
	
	#if UNITY_EDITOR
	public static			Object?				LoadEditorAsset			( this AssetRef @ref )						=> AssetsLoader.EditorLoadAsset( @ref, typeof(Object) );
	public static			Object?				LoadEditorAsset<T>		( this AssetRef<T> @ref ) where T : Object	=> AssetsLoader.EditorLoadAsset( @ref, typeof(T) );
	#endif
}