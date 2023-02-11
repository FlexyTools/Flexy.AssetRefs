using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	public class AssetResolver : AssetRefResolver
	{
		public override async UniTask<T>	LoadAssetAsync<T>		( String address, IProgress<Single> progress )	
		{
			if ( AssetRef.AllowDirectAccessInEditor )
				return (T)EditorLoadAsset( address, typeof(T) );
			
			var asset	= (ResourceRef) await UnityEngine.Resources.LoadAsync<ResourceRef>( $"AssetRefs/{address}" );
			return (T)asset.Ref;
		}
		public override T					LoadAssetSync<T>		( String address )								
		{
			if( address[3] == ':' )
				address = address[4..];
			
			if( AssetRef.AllowDirectAccessInEditor  )
				return (T)EditorLoadAsset( address, typeof(T) );
			
			var asset	= UnityEngine.Resources.Load<ResourceRef>( $"AssetRefs/{address}" );
			return (T)asset.Ref;
		}
		public override UniTask				DownloadDependencies	( String address, IProgress<Single> progress )	
		{
			return UniTask.CompletedTask;
		}
		public override UniTask<Int32>		GetDownloadSize			( String address )								
		{
			return UniTask.FromResult(0);
		}
		
		public override Object				EditorLoadAsset			( String address, Type type )					
		{
			#if UNITY_EDITOR
			if ( String.IsNullOrEmpty( address ) || String.Equals( address, "null", StringComparison.Ordinal ) )
				return null;

			if( address.Length == 32 ) //pure giud
			{
				var path = UnityEditor.AssetDatabase.GUIDToAssetPath( address );
			
				return UnityEditor.AssetDatabase.LoadAssetAtPath( path, type );
			}
			else
			{
				var guid		= address.AsSpan( )[..32].ToString( );
				var instanceId	= Int64.Parse( address.AsSpan( )[33..] ); 
				var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( guid );
				
				foreach ( var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath( path ) )
				{
					if( UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out long instanceId2 ) )
					{
						if( instanceId == instanceId2 )
							return asset;
					}
				}
				
				
			}
			#endif
			
			return null;
		}
		public override String				EditorCreateAssetAddress( Object asset )								
		{
			#if UNITY_EDITOR
			
			if( UnityEditor.AssetDatabase.IsMainAsset( asset ) && UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid, out long instanceId ) )
				return $"{guid}";	
			
			if( UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out long instanceId2 ) )
				return $"{guid2}_{instanceId2}";
			
			#endif
			
			return "";
		}
		
		// public static Object	LoadAssetBypassBungles				( String assetGuid, String subObjectName )		
		// {
		// 	if( String.IsNullOrEmpty( assetGuid ) )
		// 		return null;
		// 	
		// 	var path = AssetDatabase.GUIDToAssetPath( assetGuid );
		// 	var assetAtPath = AssetDatabase.LoadAssetAtPath( path, typeof(Object) );
		// 	
		// 	if ( assetAtPath is SceneAsset || String.IsNullOrEmpty( subObjectName ) )
		// 		return assetAtPath;
		// 	
		// 	var allAssetsAtPath   = AssetDatabase.LoadAllAssetsAtPath( path ).Where( o => o !=null && (AssetDatabase.IsMainAsset( o ) || (AssetDatabase.IsSubAsset( o ) && !(o is GameObject))) ).OrderBy( AssetDatabase.IsMainAsset ).ToList(  );
		// 	
		// 	if (allAssetsAtPath.Count ==0)
		// 		return null;
		// 	
		// 	var resultAsset = allAssetsAtPath.FirstOrDefault(x => x.name == subObjectName);
		// 	return resultAsset;
		// }
	}
}