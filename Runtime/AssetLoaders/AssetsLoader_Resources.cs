using System.IO;

namespace Flexy.AssetRefs.AssetLoaders;

public class AssetsLoader_Resources : AssetsLoader
{
	protected override		Int64					Package_GetDownloadBytes_Impl( AssetRef @ref )		=> 0;
	protected override		LoadTask<Boolean>		Package_DownloadAsync_Impl	( AssetRef @ref )		=> LoadTask.FromResult(true);

	protected override async UniTask<T>				LoadAssetAsync_Impl<T>		( AssetRef @ref )		
	{
		var resourceRef	= (ResourceRef) await Resources.LoadAsync<ResourceRef>( $"AssetRefs/{@ref}" );

		if (resourceRef) 
		{
			var result	= resourceRef.Ref; 
			if ( result is Sprite sprite )
				await UniTask.WaitWhile( ( ) => !sprite.texture ).Timeout( TimeSpan.FromSeconds(10) );
			
			if( result == null )
				throw new ArgumentException( $"[AssetRef] - Resources Loader - Loading asset: {@ref} failed", "@ref" );
			
			return (T)result;
		}
			
		if( typeof(T).IsSubclassOf(typeof(MonoBehaviour)) )
		{
			var croppedAddress = new AssetRef( @ref.Uid, default );
			resourceRef	= (ResourceRef) await Resources.LoadAsync<ResourceRef>( $"AssetRefs/{croppedAddress.ToString()}" );
					
			if( !resourceRef )
				throw new ArgumentException( $"[AssetRef] - Resources Loader - Loading asset: {@ref} failed", "@ref" );
			
			var go = (GameObject?)resourceRef.Ref;
			if( go != null )
			{
				var c = go.GetComponent<T>( );
				return c;
			}
		}
					
		throw new ArgumentException( $"[AssetRef] - Resources Loader - Loading asset: {@ref} failed", "@ref" );
	}
	
	protected override		T						LoadAssetSync_Impl<T>		( AssetRef @ref )		
	{		
		var resourceRef	= Resources.Load<ResourceRef>( $"AssetRefs/{@ref}" );

		if (resourceRef) 
		{
			var result	= resourceRef.Ref; 
			
			if( result == null )
				throw new ArgumentException( $"[AssetRef] - Resources Loader - Loading asset: {@ref} failed", "@ref" );
			
			return (T)result;
		}
			
		if( typeof(T).IsSubclassOf(typeof(MonoBehaviour)) )
		{
			var croppedAddress = new AssetRef( @ref.Uid, default );
			resourceRef	= Resources.Load<ResourceRef>( $"AssetRefs/{croppedAddress.ToString()}" );
					
			if( !resourceRef )
				throw new ArgumentException( $"[AssetRef] - Resources Loader - Loading asset: {@ref} failed", "@ref" );
					
			var go = (GameObject?)resourceRef.Ref;
			if( go != null )
			{
				var c = go.GetComponent<T>( );
				return c;
			}
		}
		
		throw new ArgumentException( $"[AssetRef] - Resources Loader - Loading asset: {@ref} failed", "@ref" );
	}
		
	protected override		String					GetSceneName_Impl			( SceneRef @ref )								
	{
		var address		= @ref.Uid;
		var asset		= Resources.Load<ResourceRef>( $"AssetRefs/{address}" );
			
		return asset.Name ?? "";
	}
	protected override		SceneTask				LoadSceneAsync_Impl			( SceneRef @ref, SceneTask.Parameters p )		
	{
		var address			= @ref.Uid;
		var asset			= Resources.Load<ResourceRef>( $"AssetRefs/{address}" );
		var sceneLoadOp		= SceneManager.LoadSceneAsync( asset.Name, new LoadSceneParameters( p.LoadMode, p.PhysicsMode ) );
		sceneLoadOp.allowSceneActivation = p.ActivateOnLoad;
		sceneLoadOp.priority = p.Priority;
		var scene			= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );	
		
		var info			= SceneTask.GetSceneInfo( );
		info.Scene			= scene;
		info.DelaySceneActivation = !p.ActivateOnLoad;
		
		return new( SceneLoadWaitImpl( sceneLoadOp, info ), info );
	}
	
	#if UNITY_EDITOR
	public class ResourcesPopulateRefs : IRefsProcessor
	{
		public void Process( RefsCollector collector, RefsCollector.List refs, Boolean isPreview )
		{
			if( isPreview )
				return;

			Debug.Log			( $"[ResourcesIRefSourceBuilder] - CreateResourcesAssetForeachAssetRefSource" );
		
			Directory.CreateDirectory( "Assets/Resources/AssetRefs" );
		
			try						
			{
				UnityEditor.AssetDatabase.StartAssetEditing( );
		
				var ress = refs;
			
				foreach ( var r in ress )
				{
					if ( !r )
					{
						Debug.LogError( $"[ResourcesIRefSourceBuilder] - CreateResourcesAssetForeachAssetRefSource: resource is null in {collector.name} collector. Skipped", collector );
						continue;
					}

					var rref = ScriptableObject.CreateInstance<ResourceRef>( );
					rref.Ref = r;
					var assetAddress	= EditorGetAssetAddress( r );
				
					if( r is UnityEditor.SceneAsset sa )
						rref.Name = sa.name;
					
					try						{ UnityEditor.AssetDatabase.CreateAsset( rref, $"Assets/Resources/AssetRefs/{assetAddress}.asset" ); }
					catch (Exception ex)	{ Debug.LogException( ex ); }
				}
			}
			finally
			{
				UnityEditor.AssetDatabase.StopAssetEditing( );
			}
		}
	}
	#endif
}