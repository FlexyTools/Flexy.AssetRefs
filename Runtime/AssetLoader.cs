using Flexy.AssetRefs.Extra;

namespace Flexy.AssetRefs;

public abstract class AssetLoader
{
	public		 			UniTask<T?>				LoadAssetAsync<T>			( AssetRef @ref ) where T:Object		
	{
		if ( @ref.IsNone )
			return UniTask.FromResult<T?>( null );
		
		try
		{
#if UNITY_EDITOR
			if (!EditorBehaviourAndMenu.RuntimeBehaviorEnabled || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return EditorLoadAsync(@ref);
				
				static async UniTask<T?> EditorLoadAsync		( AssetRef @ref )
				{
					var asset = EditorLoadAsset(new AssetRef<T>(@ref.Uid, @ref.SubId));
					await UniTask.NextFrame(PlayerLoopTiming.EarlyUpdate);
					return asset;
				}
			}
#endif	
			
			return LoadAssetAsync_Impl<T>(@ref);
		}
		catch( Exception ex )
		{
			Debug.LogException(ex);
			return UniTask.FromResult<T?>(null);
		}
	}
	public 					T?						LoadAssetSync<T>			( AssetRef @ref ) where T:Object		
	{
		if (@ref.IsNone)
			return null;
		
		try
		{
#if UNITY_EDITOR
			if (!EditorBehaviourAndMenu.RuntimeBehaviorEnabled || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
				return EditorLoadAsset(new AssetRef<T>(@ref.Uid, @ref.SubId));
#endif
			
			return LoadAssetSync_Impl<T>(@ref);
		}
		catch( Exception ex )
		{
			Debug.LogException(ex);
			return null;
		}
	}
	
	public	static			T?						EditorLoadAsset<T>			( AssetRef<T> address ) where T : Object
	{
		var asset = EditorLoadAssetRaw	(address);
		
		if (asset is GameObject go && typeof(T).IsSubclassOf(typeof(Component)))
			return go.GetComponent<T>();
		
		return (T?)asset;
	}
	public	static			Object?					EditorLoadAssetRaw			( AssetRef address )					
	{
#if UNITY_EDITOR
		
		if (address.IsNone)
			return null;

		if (address.SubId == 0) //pure guid
		{
			var path = UnityEditor.AssetDatabase.GUIDToAssetPath( address.Uid.ToGUID() );
		
			return UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
		}
		else
		{
			var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( address.Uid.ToGUID() );
			
			foreach ( var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path) )
			{
				if (!asset || !UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out Int64 instanceId )) 
					continue;
				
				if (address.SubId == instanceId)
					return asset;
			}
		}
#endif
		
		return null;
	}
	public	static			AssetRef				EditorGetAssetAddress		( Object asset )						
	{
		if (!asset)
			return default;
		
#if UNITY_EDITOR
		
		if ((asset is Component or GameObject or ScriptableObject || UnityEditor.AssetDatabase.IsMainAsset(asset)) && UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid, out Int64 _ ))
			return new( new UnityEditor.GUID(guid).ToHash(), 0 );	
		
		if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out long instanceId ))
			return new( new UnityEditor.GUID(guid2).ToHash(), instanceId );
		
#endif
		
		return default;
	}
	
	// Virtual interface for loding customisation
	protected abstract		UniTask<T?>				LoadAssetAsync_Impl<T>		( AssetRef @ref ) where T:Object;
	protected abstract		T?						LoadAssetSync_Impl<T>		( AssetRef @ref ) where T:Object;
}