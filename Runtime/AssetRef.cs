global using System;
global using System.Collections.Generic;
global using Cysharp.Threading.Tasks;
global using UnityEngine;
global using UnityEngine.SceneManagement;
global using UnityEngine.Pool;
global using Object = UnityEngine.Object;

using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Flexy.AssetRefs.AssetLoaders;
using Flexy.Serialisation;
using UnityEngine.U2D;

namespace Flexy.AssetRefs 
{
	//TODO: Add ability to load many assetrefs on one go. Like load entire bundle of assets and register all loaded assetrefs for fast access for 100500 assets it must be faster than loading 1 by 1
	
	//[DebuggerDisplay("{ToString()} = {AssetsLoader.EditorLoadAsset()}")]
	[Serializable]
	public struct AssetRef<T> : IRefLike, ISerializeAsString, IEquatable<AssetRef<T>> where T: Object
	{
		public	AssetRef ( Hash128 uid, Int64 subId = 0 )	{ _uid = uid; _subId = subId; }
		public	AssetRef ( String refAddress )				{ this = default; FromString( refAddress ); }
		
		[SerializeField] Hash128		_uid;
		[SerializeField] Int64			_subId;
		
		public static	AssetRef<T>		None				=> default;
		
		public			Hash128			Uid					=> _uid;
		public			Int64			SubId				=> _subId;
		public			Boolean			IsNone				=> this == default;
		public			AssetRef		Raw					=> this;
		
		public			Boolean			Like				( String cropId ) => _uid.ToString().StartsWith( cropId );
		
		public override Int32			GetHashCode			( )										=> _uid.GetHashCode() ^ _subId.GetHashCode( );
		public override Boolean			Equals				( System.Object obj )					=> obj is AssetRef<T> ar && this == ar;
		public			Boolean			Equals				( AssetRef<T> other )					=> _uid == other._uid & _subId == other._subId;
		public static	Boolean			operator ==			( AssetRef<T> left, AssetRef<T> right )	=> left._uid == right._uid & left._subId == right._subId;
		public static	Boolean			operator !=			( AssetRef<T> left, AssetRef<T> right )	=> !(left == right);
		
		public override	String			ToString			( ) => _uid == default ? String.Empty : _subId == 0 ? $"{_uid}" : $"{_uid}[{_subId}]";
		public 			void			FromString			( String address )							
		{
			if( String.IsNullOrWhiteSpace( address ) )
			{
				this = default;
				return;
			}
			
			_uid		= Hash128.Parse( address[..32] ); 
			_subId		= address.Length == 32 ? 0 : Int64.Parse(address[33..^1]);
		}

		public static implicit operator AssetRef			( AssetRef<T> art )	=> new( art._uid, art._subId );
	}
	
	[Serializable]
	public struct SceneRef : IRefLike, ISerializeAsString, IEquatable<SceneRef>, ISerializationCallbackReceiver
	{
		public	SceneRef ( Hash128 uid )		{ _uid = uid; _addressableName = default; }
		public	SceneRef ( String refAddress )	{ this = default; FromString( refAddress ); }
		
		[SerializeField] Hash128		_uid;
		[SerializeField] String?		_addressableName;

		public static	SceneRef		None					=> default;
		
		public			Hash128			Uid						=> _uid;
		public			Boolean			IsNone					=> this == default;
		public			AssetRef		Raw						=> (AssetRef)this;
		public			String			SceneName				=> AssetRef.AssetsLoader.GetSceneName( this );

		public override Int32			GetHashCode				( )									=> _uid.GetHashCode();
		public override Boolean			Equals					( System.Object obj )				=> obj is SceneRef sr && this == sr;
		public			Boolean			Equals					( SceneRef other )					=> _uid == other._uid;
		public static	Boolean			operator ==				( SceneRef left, SceneRef right )	=> left._uid == right._uid;
		public static	Boolean			operator !=				( SceneRef left, SceneRef right )	=> !(left == right);
		
		public override	String			ToString				( )					=> _uid == default ? String.Empty : _uid.ToString( );
		public			void			FromString				( String address )	=> _uid = String.IsNullOrWhiteSpace( address ) ? default : Hash128.Parse( address );

		public static	SceneTask		LoadDummyScene			( GameObject ctx, LoadSceneMode mode, UnloadSceneOptions unloadOptions = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects ) => AssetRef.AssetsLoader.LoadDummyScene( ctx, mode, unloadOptions );
		
		public static explicit operator AssetRef				( SceneRef sr )		=> new( sr._uid, 0 );
		
		public void OnBeforeSerialize()
		{
			
		}
		public void OnAfterDeserialize()
		{
			if( _uid == default && !String.IsNullOrWhiteSpace( _addressableName ) )
				this = new( _addressableName );
		}
	}
	
	[Serializable] 
	public struct AssetRef : IRefLike, IEquatable<AssetRef>, ISerializationCallbackReceiver
	{
		public	AssetRef	( Hash128 refAddress, Int64 subId ) { this = default; _uid = refAddress; _subId = subId; }
		public	AssetRef	( String refAddress )				{ this = default; FromString( refAddress ); }
		
		[SerializeField] Hash128	_uid;
		[SerializeField] Int64		_subId;
		[SerializeField] String?	_addressableName;
		[SerializeField] String?	_subObjectName;

		public static	AssetRef	None		=> default;
		
		public			Hash128		Uid			=> _uid;
		public			Int64		SubId		=> _subId;
		
		public			Boolean		IsNone		=> _uid == default;

#if UNITY_EDITOR
		public  Object?		EditorAsset	=> AssetsLoader.EditorLoadAsset( this, typeof(Object) );
#endif

		public override Int32			GetHashCode			( )									=> _uid.GetHashCode() ^ _subId.GetHashCode( );
		public override Boolean			Equals				( System.Object obj )				=> obj is AssetRef ar && this == ar;
		public			Boolean			Equals				( AssetRef other )					=> _uid == other._uid & _subId == other._subId;
		public static	Boolean			operator ==			( AssetRef left, AssetRef right )	=> left._uid == right._uid & left._subId == right._subId;
		public static	Boolean			operator !=			( AssetRef left, AssetRef right )	=> !(left == right);
		
		public override	String			ToString			( )									=> _uid == default ? String.Empty : _subId == 0 ? $"{_uid}" : $"{_uid}[{_subId}]";
		public 			void			FromString			( String address )					
		{
			if( String.IsNullOrWhiteSpace( address ) )
			{
				this = default;
				return;
			}

			var uid		= Hash128.Parse( address[..32] ); 
			var subId	= address.Length == 32 ? 0 : Int64.Parse(address[33..^1]);
			
			this = new( uid, subId );
		}
		
		public	static	AssetsLoader	AssetsLoader		= new AssetsLoader_Resources( );

		public T			LoadAssetTypedSync<T>( )where T : Object	=> new AssetRef<T>( _uid, _subId ).LoadAssetSync( );
		public UniTask<T>	LoadAssetTyped<T>( )	where T : Object	=> new AssetRef<T>( _uid, _subId ).LoadAssetAsync( );
		
		public void OnBeforeSerialize()
		{
			
		}
		public void OnAfterDeserialize()
		{
			#if UNITY_EDITOR
			if( _uid == default )
			{
				if( !String.IsNullOrWhiteSpace( _addressableName ) )
				{
					if( !String.IsNullOrWhiteSpace( _subObjectName ) )
					{
						unsafe
						{
							var ptr = Unsafe.AsPointer( ref this );
							{
								UnityEditor.EditorApplication.update += new AssetRefResolver { RefPtr = ptr }.Resolve;
								//UnityEditor.EditorApplication.QueuePlayerLoopUpdate( );
							}
						}
					}
					else
					{
						var guid = _addressableName;
						this = new( _addressableName );
						_addressableName = guid;
					}
				}
			}
			#endif
		}
		#if UNITY_EDITOR
		public unsafe class AssetRefResolver
		{
			public void* RefPtr;
		
			public void	Resolve	( )		
			{
				UnityEditor.EditorApplication.update -= Resolve;
				UnityEditor.EditorApplication.update -= Resolve;
				ref var assetRef	= ref Unsafe.AsRef<AssetRef>(RefPtr);
				
				//Debug.Log( $"[AsserRef Resolver] Try resolve: {assetRef._addressableName} {assetRef._subObjectName}" );
				
				var assetGuid		= assetRef._addressableName;
				var subObjectName	= assetRef._subObjectName;
				
				var obj = EditorLoadAssetWithSubObjectName( assetGuid!, subObjectName! );
				
				//Debug.Log( $"[AsserRef Resolver] Resolved to Object: {obj}" );
				
				if( obj != null )
				{
					assetRef = AssetsLoader.EditorGetAssetAddress( obj );
					assetRef._addressableName=assetGuid;
					assetRef._subObjectName=subObjectName;
				}
			}
			
			public  Object?	EditorLoadAssetWithSubObjectName	( String assetGuid, String subObjectName )
			{
				var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( assetGuid );
				var assetAtPath	= UnityEditor.AssetDatabase.LoadAssetAtPath( path, typeof(Object) );

				if (assetAtPath is SpriteAtlas atlas)
				{
					if (String.IsNullOrEmpty(subObjectName)) return assetAtPath;
					
					foreach (var packable in UnityEditor.U2D.SpriteAtlasExtensions.GetPackables(atlas))
					{
						if (packable is UnityEditor.DefaultAsset )
						{
							var assetPath  = UnityEditor.AssetDatabase.GetAssetPath( packable );
							if (String.IsNullOrEmpty( Path.GetExtension( assetPath )))
							{
								var assets = GetAssetsAtPath<Sprite>( assetPath, true );
								foreach ( var sprite in assets )
								{
									if (sprite.name == subObjectName) return sprite; 
								}
							}
						}
						
						if (packable.name == subObjectName) return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GetAssetPath(packable));
					}
					
					return null;
				}

				if ( assetAtPath is UnityEditor.SceneAsset || String.IsNullOrEmpty( subObjectName ) )
					return assetAtPath;
				
				var allAssetsAtPath   = UnityEditor.AssetDatabase.LoadAllAssetsAtPath( path ).Where( o => o !=null && (UnityEditor.AssetDatabase.IsMainAsset( o ) || (UnityEditor.AssetDatabase.IsSubAsset( o ) && !(o is GameObject))) ).OrderBy( UnityEditor.AssetDatabase.IsMainAsset ).ToList(  );
				
				if (allAssetsAtPath.Count ==0)
					return null;
				
				var resultAsset = allAssetsAtPath.FirstOrDefault(x => x.name == subObjectName);
				return resultAsset;
				
				static  T[] GetAssetsAtPath<T> (String path, Boolean withSubFolders) 
				{
					List<T> result = new List<T>();
					var filesToAdd = Directory.GetFiles(path, "*", withSubFolders?SearchOption.AllDirectories:SearchOption.TopDirectoryOnly);
					foreach(String filePath in filesToAdd)
					{
						Object item = UnityEditor.AssetDatabase.LoadAssetAtPath(filePath.Replace('\\', '/'), typeof(T));
	            
						if (item != null && item is T tItem)
							result.Add( tItem );
					}

					return result.ToArray(  );
				}		
			}
		}
		#endif
		public static Boolean ValidateAssetRef(AssetRef fieldValue, out AssetRef _)
		{
			_ = default;
			return false;
		}
	}
	
	public interface IRefLike
	{
		public	Hash128		Uid		{ get; }
		public	Int64		SubId	=> default;
	}
	
	[AttributeUsage(AttributeTargets.Field)]
	public class AssetTypeAttribute : PropertyAttribute
	{
		public AssetTypeAttribute ( Type assetType )
		{
			AssetType = assetType;
		}

		public Type AssetType { get ; }
	}
}