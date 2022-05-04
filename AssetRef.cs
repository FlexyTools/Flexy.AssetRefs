using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs 
{
	[Serializable]
	public struct AssetRef<T>
	{
		public	AssetRef ( String refAddress )	
		{
			_refAddress = refAddress;
		}
		
		// address form is 3 or 4 symbols of global manager id that will handle reference then ':' then string part manager can understand
		[SerializeField] String			_refAddress;
		
		public			Boolean			IsNone				=> String.IsNullOrEmpty( _refAddress );
		
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
		
		public			T				LoadAssetSync		( )										
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
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init( )
		{
			_registeredResolvers.Clear( );
			_sceneResolver		= new ScnResolver( );
			_defaultResolver	= new PkgResolver( );
		}
		
		private		static			List<(String, AssetRefResolver)>		_registeredResolvers	= new ( );
		private		static			AssetRefResolver						_defaultResolver;
		private		static			ScnResolver								_sceneResolver;
		
		public		static			ScnResolver			GetSceneResolver	( )						
		{
			if( _sceneResolver == null )
				Editor.RegisterResolversInEditor( );
				
			return _sceneResolver;
		}
		public		static			AssetRefResolver	GetResolver			( String refAddress )	
		{
			if( String.IsNullOrEmpty( refAddress ) || refAddress.Length < 3 )
				return null;
				
			var resolverId = refAddress[3] == ':' ? refAddress.AsSpan(0, 3) : refAddress.AsSpan(0, 4);

			#if UNITY_EDITOR
			if( _registeredResolvers.Count == 0 )
				Editor.RegisterResolversInEditor( );
			#endif
			
			foreach ( var resolver in _registeredResolvers )
			{
				if( resolverId.CompareTo(resolver.Item1.AsSpan(), StringComparison.Ordinal) == 0 )
					return resolver.Item2;
			}

			//Debug.LogError		( $"[AssetRef] - GetManager: RefResolver id {resolverId.ToString( )} Unregistered" );
			return _defaultResolver;
		}
		public		static			void				RegisterResolver	( AssetRefResolver resolver )
		{
			Debug.Log			( $"[AssetRef] - RegisterResolver: {resolver}" );
			_registeredResolvers.Add( (resolver.Prefix, resolver)  );
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
				_sceneResolver = new ScnResolver( );
				
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
}