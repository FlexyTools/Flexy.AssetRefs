using System.IO;
using Flexy.AssetRefs.Pipelines;

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

		if( !resourceRef )
			resourceRef		= Resources.Load<ResourceRef>( $"Fun.Flexy/AssetRefs/{@ref.Uid.ToString()}" );
		
		return LoadFinalising<T>( resourceRef.Ref );
	}
	
	protected override		String					GetSceneName_Impl			( SceneRef @ref )							
	{
		var address		= @ref.Uid;
		var asset		= Resources.Load<ResourceRef>( $"Fun.Flexy/AssetRefs/{address}" );
			
		return asset.Name ?? "";
	}
	protected override		SceneTask				LoadSceneAsync_Impl			( SceneRef @ref, SceneTask.Parameters p )	
	{
		var address			= @ref.Uid;
		var asset			= Resources.Load<ResourceRef>( $"Fun.Flexy/AssetRefs/{address}" );
		var sceneLoadOp		= SceneManager.LoadSceneAsync( asset.Name, new LoadSceneParameters( p.LoadMode, p.PhysicsMode ) );
		sceneLoadOp.allowSceneActivation = p.ActivateOnLoad;
		sceneLoadOp.priority = p.Priority;
		var scene			= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );	
		
		var info			= SceneTask.GetSceneData( );
		info.Scene			= scene;
		info.DelaySceneActivation = !p.ActivateOnLoad;
		
		return new( SceneLoadWaitImpl( sceneLoadOp, info ), info );
	}

	private					T?						LoadFinalising<T>			( Object? obj ) where T : Object			
	{
		var result	= obj; 
		
		if( result is GameObject go && typeof(T).IsSubclassOf(typeof(MonoBehaviour)) )
			return go.GetComponent<T>( );
					
		return (T?)result;
	}
	
	#if UNITY_EDITOR
	public class ResourcesPopulateRefs : IPipelineTask
	{
		public void Run( Pipeline ppln, Context ctx )
		{
			Debug.Log			( $"[ResourcesIRefSourceBuilder] - CreateResourcesAssetForeachAssetRefSource" );
		
			Directory.CreateDirectory( "Assets/Resources/Fun.Flexy/AssetRefs" );
		
			try						
			{
				var refs = ctx.Get<RefsList>( );
				
				UnityEditor.AssetDatabase.StartAssetEditing( );
		
				var ress = refs;
			
				foreach ( var r in ress )
				{
					if ( !r )
					{
						Debug.LogError( $"[ResourcesIRefSourceBuilder] - CreateResourcesAssetForeachAssetRefSource: resource is null in {ppln.name} collector. Skipped", ppln );
						continue;
					}

					var rref = ScriptableObject.CreateInstance<ResourceRef>( );
					rref.Ref = r;
					var assetAddress	= EditorGetAssetAddress( r );
				
					if( r is UnityEditor.SceneAsset sa )
						rref.Name = sa.name;
					
					try						{ UnityEditor.AssetDatabase.CreateAsset( rref, $"Assets/Resources/Fun.Flexy/AssetRefs/{assetAddress}.asset" ); }
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