namespace Flexy.AssetRefs.AssetLoaders;

public class AssetLoaderResources : AssetLoader
{
	protected override async UniTask<T?>	LoadAssetAsync_Impl<T>		( AssetRef @ref )	where T : class		
	{
		var resourceRef	= (ResourceRef) await Resources.LoadAsync<ResourceRef>( $"Fun.Flexy/AssetRefs/{@ref}" );

		if( !resourceRef )
			resourceRef		= (ResourceRef) await Resources.LoadAsync<ResourceRef>( $"Fun.Flexy/AssetRefs/{@ref.Uid.ToString()}" );
		
		if( !resourceRef )
		{
			Debug.LogError( $"[AssetsLoader] Resources - RefFile is absent for: {@ref}" );
			return null;
		}
		
		if ( resourceRef.Ref is Sprite sprite )
		{
			await UniTask.WaitWhile( ( ) => !sprite.texture ).Timeout( TimeSpan.FromSeconds(10) );
			return (T?)resourceRef.Ref;
		}
		
		return LoadFinalising<T>( resourceRef.Ref );
	}
	protected override		T?				LoadAssetSync_Impl<T>		( AssetRef @ref )	where T : class		
	{		
		var resourceRef	= Resources.Load<ResourceRef>( $"Fun.Flexy/AssetRefs/{@ref}" );

		if (!resourceRef)
			resourceRef		= Resources.Load<ResourceRef>( $"Fun.Flexy/AssetRefs/{@ref.Uid.ToString()}" );
		
		if (!resourceRef)
		{
			Debug.LogError( $"[AssetsLoader] Resources - RefFile is absent for: {@ref}" );
			return null;
		}
		
		return LoadFinalising<T>(resourceRef.Ref);
	}
	private					T?				LoadFinalising<T>			( Object? obj )		where T : Object	
	{
		var result	= obj; 
		
		if (result is GameObject go && typeof(T).IsSubclassOf(typeof(Component)))
			return go.GetComponent<T>();
					
		return (T?)result;
	}
}