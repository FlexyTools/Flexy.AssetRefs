using System.IO;
using Flexy.AssetRefs.Extra;

namespace Flexy.AssetRefs
{
	public class ContentService_Resources : ContentService
	{
		public override async	UniTask<T?>		LoadAssetAsync<T>		( AssetRef @ref )	where T : class		
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
		
			return GetSpecializedAsset<T>( resourceRef.Ref );
		}
		public override			T?				LoadAssetSync<T>		( AssetRef @ref )	where T : class		
		{		
			var resourceRef		= Resources.Load<ResourceRef>( $"Fun.Flexy/AssetRefs/{@ref}" );

			if (!resourceRef)
				resourceRef		= Resources.Load<ResourceRef>( $"Fun.Flexy/AssetRefs/{@ref.Uid.ToString()}" );
		
			if (!resourceRef)
			{
				Debug.LogError( $"[AssetsLoader] Resources - RefFile is absent for: {@ref}" );
				return null;
			}
		
			return GetSpecializedAsset<T>(resourceRef.Ref);
		}
		public override			String			GetSceneName			( SceneRef @ref )						
		{
			var asset		= Resources.Load<ResourceRef>($"Fun.Flexy/AssetRefs/{@ref.Uid}");
			
			return asset.Name ?? "";
		}
		public override			LoadSceneTask	LoadSceneAsync			( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context )	
		{
			var asset			= Resources.Load<ResourceRef>($"Fun.Flexy/AssetRefs/{@ref.Uid}");
	
#if UNITY_EDITOR
			var path			= UnityEditor.AssetDatabase.GUIDToAssetPath( @ref.Uid.ToGUID() );
			if (asset == null || Path.GetFileNameWithoutExtension(path) != asset.Name)
				throw new InvalidOperationException($"AssetRef {@ref} is invalid.");
		
			var sceneLoadOp		= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new(p.LoadMode, p.PhysicsMode) );
#else
		var sceneLoadOp		= SceneManager.LoadSceneAsync( asset.Name, new LoadSceneParameters( p.LoadMode, p.PhysicsMode ) );
#endif
		
			var scene		= SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
			var sceneTask	= new LoadSceneTask(context, @ref, p, scene);
		
			return sceneTask.Run( WaitSceneLoad(sceneLoadOp, sceneTask) );
		}
	}
}