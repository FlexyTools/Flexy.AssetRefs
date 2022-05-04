using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using AssetDatabase = UnityEditor.AssetDatabase;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace Flexy.AssetRefs 
{
	public interface  IAssetRefsSource
	{
		List<Object> CollectAssets( );
	}
	
	[Serializable]
	public struct AssetRef_Scene
	{
		public AssetRef_Scene ( String refAddress )
		{
			_refAddress = refAddress;
		}
		
		//load asset and then convert it to exact type
		[SerializeField] String _refAddress;
		
		// address form is 3 or 4 symbols of global manager id that will handle reference then ':' then string part manager can understand 
		// some valid predefined forms
		// http://d1l9wtg77iuzz5.cloudfront.net/assets/5501/252869/original.jpg - manager that resolve asset from web
		// raw:some-image-in-Rersources-folder.jpg - manager that resolve asset in raw form from resources 
		// pkg:40B0C618-0489-4035-86C6-4B971CF735E0:25000027 - manager that will load imported unity resource from any location (Bundle, Resources, Somewhere else) 
		// gdi:02F7179A-5061-440D-951B-539603FE0158:World.Map.12 - manager that return gdi object by guid or EntityId
		// scn:01257675-C281-472E-A774-A69DBA7D5D82:Map.Island.12 - manager that can load scene by name
		
		public	Boolean IsNone			=> String.IsNullOrEmpty( _refAddress );
		
		public	async	UniTask			DownloadDependencies( IProgress<Single> progress = null )	
		{
			var resolver	= AssetRef.GetResolver( _refAddress );
			await resolver.DownloadDependencies( _refAddress, progress );
		}
		public	async	UniTask<Int32>	GetDownloadSize		( )										
		{
			var resolver	= AssetRef.GetResolver( _refAddress );
			return await resolver.GetDownloadSize( _refAddress );
		}
		
		public 			UniTask<Scene> 	LoadSceneAsync		( IProgress<Single> progress = null )	
		{
			var resolver	= AssetRef.GetResolver( _refAddress );
			return ((ScnResolver)resolver).LoadSceneAsync( _refAddress, progress );
		}
		
		// public static async UniTask<AsyncOperation> LoadScene(String sceneName, Boolean allowSceneActivation = true, LoadSceneMode loadMode = LoadSceneMode.Single, IProgress<Single> progress = null )
		// {
		// 	return await BundleManager.LoadSceneAsync( sceneName, allowSceneActivation, loadMode, progress );
		// }
		// public async UniTask<AsyncOperation> LoadScene(Boolean allowSceneActivation = true,LoadSceneMode loadMode = LoadSceneMode.Single, IProgress<Single> progress = null )
		// {
		// 	return await LoadScene( _sceneName, allowSceneActivation, loadMode, progress);
		// }
	}
	
	[Serializable]
	public struct AssetRef<T>
	{
		public AssetRef ( String refAddress )
		{
			_refAddress = refAddress;
		}
		
		// address form is 3 or 4 symbols of global manager id that will handle reference then ':' then string part manager can understand
		[SerializeField] String _refAddress;
		
		public	Boolean IsNone			=> String.IsNullOrEmpty( _refAddress );
		
		public	async	UniTask			DownloadDependencies( IProgress<Single> progress = null )	
		{
			var resolver	= AssetRef.GetResolver( _refAddress );
			await resolver.DownloadDependencies( _refAddress, progress );
		}
		public	async	UniTask<Int32>	GetDownloadSize		( )										
		{
			var resolver	= AssetRef.GetResolver( _refAddress );
			return await resolver.GetDownloadSize( _refAddress );
		}
		
		public T LoadAssetSync( )
		{
			var resolver	= AssetRef.GetResolver( _refAddress );
			var asset		= resolver.LoadAssetSync( _refAddress );
			
			if( asset is T tr )
				return tr;
			
			if ( TryTypedOf( asset, out T result ) )
				return result;
			
			return default;
		}
		public async	UniTask<T> 		LoadAssetAsync		( IProgress<Single> progress = null )	
		{
			var resolver	= AssetRef.GetResolver( _refAddress );
			var asset		= await resolver.LoadAssetAsync( _refAddress, progress );
			
			if( asset is T tr )
				return tr;
			
			if ( TryTypedOf( asset, out T result ) )
				return result;

			if ( result is Sprite sprite )
				await UniTask.WaitWhile( ( ) => sprite.texture == null ).Timeout( TimeSpan.FromSeconds(10) );

			throw new InvalidCastException( message: $"asset {asset.name} is {asset.GetType(  )}, can not cast to {typeof(T)}" );
		}
		
		private			Boolean 		TryTypedOf			( Object obj, out T result)				
		{
			result = default;
			
			if ( obj == null )
				return false;
			
			if( typeof(MonoBehaviour).IsAssignableFrom( typeof(T) ) )
			{
				result = ((GameObject)obj).GetComponent<T>( );
				return	true;
			}

			if ( typeof(T) == typeof(Sprite) && obj is Texture2D texture )
			{
				result = (T) Convert.ChangeType(Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f), typeof(T));
				return true;
			}

			if ( obj is T tObj )
			{
				result = tObj;
				return true;
			}
			
			return false;
		}

		
	}

	public static class AssetRef
	{
		private		static			List<(String, AssetRefResolver)>		_registeredResolvers					= new ( );
		private		static			AssetRefResolver						_defaultResolver;
		
		public		static			AssetRefResolver	GetResolver			( String refAddress )					
		{
			if( String.IsNullOrEmpty( refAddress ) || refAddress.Length < 3 )
				return null;
				
			var resolverId = refAddress[3] == ':' ? refAddress.AsSpan(0, 3) : refAddress.AsSpan(0, 4);

			if( _registeredResolvers.Count == 0 )
				Editor.RegisterResolversInEditor( );
			
			foreach ( var resolver in _registeredResolvers )
			{
				if( resolverId.CompareTo(resolver.Item1.AsSpan(), StringComparison.Ordinal) == 0 )
					return resolver.Item2;
			}

			//Debug.LogError		( $"[AssetRef] - GetManager: RefResolver id {resolverId.ToString( )} Unregistered" );
			return null;
		}

		public static class Editor
		{
			public static AssetRefResolver GetResolverForType( Type type, String path )
			{
				if( _registeredResolvers.Count == 0 )
					RegisterResolversInEditor( );
				
				foreach ( var resolver in _registeredResolvers )
				{
					if( resolver.Item2.CanHandleAsset( type, path ) )
						return resolver.Item2;
				}
				
				return null;
			}

			internal static void RegisterResolversInEditor()
			{
				foreach ( var resolverType in UnityEditor.TypeCache.GetTypesDerivedFrom(typeof(AssetRefResolver)) )
				{
					if( resolverType == typeof(PkgResolver) )
					{
						_defaultResolver = (AssetRefResolver)Activator.CreateInstance( resolverType );
						continue;
					}
					
					var resolver = (AssetRefResolver)Activator.CreateInstance( resolverType );
					_registeredResolvers.Add( (resolver.Prefix, resolver) );
				}
				
				_registeredResolvers.Add( (_defaultResolver.Prefix, _defaultResolver) );
			}
		}
	}
	
	public abstract class AssetRefResolver
	{
		public abstract		String			Prefix					{ get ; }
		
		public abstract		UniTask<Object> LoadAssetAsync			( String address, IProgress<Single> progress );
		public abstract		Object			LoadAssetSync			( String address );
		
		public abstract		UniTask			DownloadDependencies	( String address, IProgress<Single> progress );
		public abstract		UniTask<Int32>	GetDownloadSize			( String address );

		public abstract		Boolean			CanHandleAsset			( Type type, String path );
		public abstract		Object			EditorLoadAsset			( String address );
		public abstract		String			EditorCreateAssetPath	( Object asset );
		
	}
	
	public class PkgResolver : AssetRefResolver
	{
		public override String Prefix => "pkg";

		public override Boolean			CanHandleAsset	( Type type, String path )
		{
			return true;
		}

		
		
		public override Object EditorLoadAsset( String address )
		{
			var guid = address.AsSpan( )[4..36].ToString( );
			var path = AssetDatabase.GUIDToAssetPath( guid );
			
			return AssetDatabase.LoadAssetAtPath<Object>( path );
		}

		public override String EditorCreateAssetPath(Object asset)
		{
			if( AssetDatabase.IsMainAsset( asset ) && AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid, out long instanceId ) )
				return $"pkg:{guid}";	
			
			if( AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out long instanceId2 ) )
				return $"pkg:{guid2}:{instanceId2}";	
			
			return "";
		}

		public override async UniTask<Object> LoadAssetAsync(String address, IProgress<Single> progress)
		{
			return EditorLoadAsset( address );
		}

		public override Object LoadAssetSync(String address)
		{
			return EditorLoadAsset( address );
		}

		public override UniTask DownloadDependencies(String address, IProgress<Single> progress)
		{
			throw new NotImplementedException();
		}

		public override UniTask<Int32> GetDownloadSize(String address)
		{
			throw new NotImplementedException();
		}
	}
	
	public class RawResolver : AssetRefResolver
	{
		public override String Prefix => "raw";

		public override Boolean CanHandleAsset(Type type, String path)
		{
			return path.StartsWith( "Assets/StreamingAssets/" );
		}

		public override Object EditorLoadAsset(String address)
		{
			return null;
		}

		public override String EditorCreateAssetPath(Object asset)
		{
			return AssetDatabase.GetAssetPath( asset ).AsSpan()["Assets/StreamingAssets".Length].ToString( );
		}

		public override UniTask<Object> LoadAssetAsync(String address, IProgress<Single> progress)
		{
			throw new NotImplementedException();
		}

		public override Object LoadAssetSync(String address)
		{
			throw new NotImplementedException();
		}

		public override UniTask DownloadDependencies(String address, IProgress<Single> progress)
		{
			throw new NotImplementedException();
		}

		public override UniTask<Int32> GetDownloadSize(String address)
		{
			throw new NotImplementedException();
		}
	}
	
	public class ScnResolver : AssetRefResolver
	{
		public override String Prefix => "scn";

		public override Boolean CanHandleAsset(Type type, String path)
		{
			return type == typeof(UnityEditor.SceneAsset) || type == typeof(Scene) ;
		}

		public override Object EditorLoadAsset(String address)
		{
			var guid = address.AsSpan( )[4..36].ToString( );
			var path = AssetDatabase.GUIDToAssetPath( guid );
			
			return AssetDatabase.LoadAssetAtPath<Object>( path );
		}

		public override String EditorCreateAssetPath(Object asset)
		{
			var guid	= AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( asset ) );
			var mapName	= Path.GetFileName( AssetDatabase.GetAssetPath( asset ) );
				
			return $"scn:{guid}:{mapName}";
		}

		public override async UniTask<Object> LoadAssetAsync(String address, IProgress<Single> progress)
		{
			throw new NotImplementedException( "Dont use it for scene, Use AssetRef_Scene" );
		}

		public override Object LoadAssetSync(String address)
		{
			throw new NotImplementedException( "Dont use it for scene, Use AssetRef_Scene" );
		}

		public override UniTask DownloadDependencies(String address, IProgress<Single> progress)
		{
			throw new NotImplementedException();
		}

		public override UniTask<Int32> GetDownloadSize(String address)
		{
			throw new NotImplementedException();
		}

		public async UniTask<Scene> LoadSceneAsync( String address, IProgress<Single> progress )
		{
			var sceneName	= address.AsSpan( )[37..].ToString( );
			var awaitable	= SceneManager.LoadSceneAsync( sceneName );
			var scene		= SceneManager.GetSceneAt( SceneManager.sceneCount );
			
			await awaitable;
			
			return scene;
		}
	}
	
	// public class GdiResolver : AssetRefResolver
	// {
	// 	public override String Prefix => "gdi";
	//
	// 	public override Boolean CanHandleAsset(Type type, String path)
	// 	{
	// 		return type == typeof(GdiObject);
	// 	}
	//
	// 	public override UniTask<Object> LoadAssetAsync(String address, IProgress<Single> progress)
	// 	{
	// 		throw new NotImplementedException();
	// 	}
	//
	// 	public override UniTask DownloadDependencies(String address, IProgress<Single> progress)
	// 	{
	// 		throw new NotImplementedException();
	// 	}
	//
	// 	public override UniTask<Int32> GetDownloadSize(String address)
	// 	{
	// 		throw new NotImplementedException();
	// 	}
	// }
	
	public class HttpResolver : AssetRefResolver
	{
		public override String Prefix => "http";

		public override Boolean CanHandleAsset(Type type, String path)
		{
			return path.StartsWith( "http://" );
		}

		public override Object EditorLoadAsset(String address)
		{
			return null;
		}

		public override String EditorCreateAssetPath(Object asset)
		{
			return null;			
		}

		public override UniTask<Object> LoadAssetAsync(String address, IProgress<Single> progress)
		{
			throw new NotImplementedException();
		}

		public override Object LoadAssetSync(String address)
		{
			throw new NotImplementedException();
		}

		public override UniTask DownloadDependencies(String address, IProgress<Single> progress)
		{
			throw new NotImplementedException();
		}

		public override UniTask<Int32> GetDownloadSize(String address)
		{
			throw new NotImplementedException();
		}
	}
}