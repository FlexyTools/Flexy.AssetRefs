namespace Flexy.AssetRefs.AssetLoaders;

public class AssetsLoader_Resources : AssetsLoader
{
	protected override async UniTask<T?>			LoadAssetAsync_Impl<T>		( AssetRef @ref ) where T : class				
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
	protected override		T?						LoadAssetSync_Impl<T>		( AssetRef @ref ) where T : class				
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
	
	protected override		String					GetSceneName_Impl			( SceneRef @ref )								
	{
		var address		= @ref.Uid;
		var asset		= Resources.Load<ResourceRef>($"Fun.Flexy/AssetRefs/{address}");
			
		return asset.Name ?? "";
	}
	protected override		LoadSceneTask			LoadSceneAsync_Impl			( SceneRef @ref, LoadSceneTask.Parameters p )	
	{
		var address			= @ref.Uid;
		var asset			= Resources.Load<ResourceRef>($"Fun.Flexy/AssetRefs/{address}");
		var sceneLoadOp		= SceneManager.LoadSceneAsync( asset.Name, new LoadSceneParameters( p.LoadMode, p.PhysicsMode ) );
		sceneLoadOp.allowSceneActivation = p.ActivateOnLoad;
		sceneLoadOp.priority = p.Priority;
		var scene			= SceneManager.GetSceneAt(SceneManager.sceneCount - 1);	
		
		var loadData		= LoadSceneTask.RentSceneLoadData();
		loadData.Scene		= scene;
		loadData.DelaySceneActivation = !p.ActivateOnLoad;
		
		return loadData.RunChain( SceneLoadWaitImpl(sceneLoadOp, loadData) );
	}

	private					T?						LoadFinalising<T>			( Object? obj ) where T : Object				
	{
		var result	= obj; 
		
		if (result is GameObject go && typeof(T).IsSubclassOf(typeof(Component)))
			return go.GetComponent<T>();
					
		return (T?)result;
	}
}