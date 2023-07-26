using System;
using Cysharp.Threading.Tasks;
using Flexy.JsonXSpace;
using Flexy.Utils.Editor;
using Flexy.Utils.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs 
{
	[Serializable]
	public struct AssetRef<T> : ISerializeAsString where T: Object
	{
		public	AssetRef ( Hash128 uid, Int32 internalId = 0 )	
		{
			_uid = uid;
			_subId = 0;
		}
		public	AssetRef ( String refAddress )	
		{
			this = default;
			FromString( refAddress );
		}
		
		[SerializeField] Hash128		_uid;
		[SerializeField] Int64			_subId;
		
		public			Boolean			IsNone				=> _uid == default;
		
		public	async	UniTask			DownloadDependencies( IProgress<Single> progress = null )	
		{
			await AssetRef.AssetResolver.DownloadDependencies( this, progress );
		}
		public	async	UniTask<Int32>	GetDownloadSize		( )										
		{
			return await AssetRef.AssetResolver.GetDownloadSize( this );
		}
		
		public			T				LoadAssetSync		( )										
		{
			var asset		= AssetRef.AssetResolver.LoadAssetSync<T>( this );
			
			if( asset is T tr )
				return tr;
			
			if ( TryTypedOf( asset, out T result ) )
				return result;
			
			return default;
		}
		public async	UniTask<T> 		LoadAssetAsync		( IProgress<Single> progress = null )	
		{
			var asset		= await AssetRef.AssetResolver.LoadAssetAsync<T>( this, progress );
			
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

		public override	String			ToString			( )				
		{
			if( _subId != 0 )
				return $"{_uid}:{_subId}";
			
			return _uid == default ? String.Empty : $"{_uid}";
		}
		public 			void			FromString			( String data )	
		{
			if( String.IsNullOrWhiteSpace( data ) )
			{
				this = default;
				return;
			}
			
			_uid = Guid.Parse( data[..32] ).ToHash(); 
			_subId = data.Length == 32 ? 0 : Int64.Parse(data[32..]);
		}

		public override Int32			GetHashCode			( )										
		{
			return _uid.GetHashCode() ^ _subId.GetHashCode( );
		}
		public override Boolean			Equals				( System.Object obj )					
		{
			if( obj is not AssetRef<T> ar )
				return false;
			
			return this == ar;
		}
		public static	Boolean			operator ==			( AssetRef<T> left, AssetRef<T> right )	
		{
			return left._uid == right._uid & left._subId == right._subId;
		}
		public static	Boolean			operator !=			( AssetRef<T> left, AssetRef<T> right )	
		{
			return !(left == right);
		}

		public static implicit operator AssetRef			( AssetRef<T> art )	=> new( art._uid, art._subId );
	}
	
	// Not mean to be serializable. Used to pass untyped asset ref around in low level code 
	public struct AssetRef
	{
		public AssetRef( Hash128 refAddress, Int64 internalId )
		{
			_uid	= refAddress;
			_subId	= internalId;
		}
		
		private readonly	Hash128		_uid;
		private readonly	Int64		_subId;

		public	Hash128		Uid			=> _uid;
		public	Int64		SubId		=> _subId;
		
		public Boolean		IsNone		=> _uid == default;
	
		public override Int32			GetHashCode			( )									
		{
			return _uid.GetHashCode() ^ _subId.GetHashCode( );
		}
		public override Boolean			Equals				( System.Object obj )				
		{
			if( obj is not AssetRef ar )
				return false;
			
			return this == ar;
		}
		public static	Boolean			operator ==			( AssetRef left, AssetRef right )	
		{
			return left._uid == right._uid & left._subId == right._subId;
		}
		public static	Boolean			operator !=			( AssetRef left, AssetRef right )	
		{
			return !(left == right);
		}
		
		public override	String			ToString			( )				
		{
			if( _subId != 0 )
				return $"{_uid}:{_subId}";
			
			return _uid == default ? String.Empty : $"{_uid}";
		}
		public 			void			FromString			( String data )	
		{
			if( String.IsNullOrWhiteSpace( data ) )
			{
				this = default;
				return;
			}

			var uid = Guid.Parse( data[..32] ).ToHash(); 
			var subId = data.Length == 32 ? 0 : Int64.Parse(data[32..]);
			
			this = new AssetRef( uid, subId );
		}
		
		public const	Boolean		IsEditor = 
			#if UNITY_EDITOR
			true;
			#else
			false;
			#endif
		
		public		static			Boolean				RuntimeBehaviorEnabled;
		public		static			Boolean				AllowDirectAccessInEditor => IsEditor && !RuntimeBehaviorEnabled;

		
		public		static			AssetRefResolver	AssetResolver = new ResourcesAssetResolver();
		
#if UNITY_EDITOR
		public static		Object			EditorLoadAsset				( AssetRef address, Type type )			
		{
			if ( address.IsNone )
				return null;

			if( address.SubId == 0 ) //pure giud
			{
				var path = UnityEditor.AssetDatabase.GUIDToAssetPath( address.Uid.ToGUID( ) );
			
				return UnityEditor.AssetDatabase.LoadAssetAtPath( path, type );
			}
			else
			{
				var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( address.Uid.ToGUID( ) );
				
				foreach ( var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath( path ) )
				{
					if ( !asset || !UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out Int64 instanceId ) ) 
						continue;
					
					if( address.SubId == instanceId )
						return asset;
				}
			}
			
			return null;
		}
		public static		AssetRef		EditorCreateAssetAddress	( Object asset )						
		{
			if( UnityEditor.AssetDatabase.IsMainAsset( asset ) && UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid, out Int64 instanceId ) )
				return new AssetRef( new UnityEditor.GUID( guid ).ToHash( ), 0 );	
			
			if( UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out long instanceId2 ) )
				return new AssetRef( new UnityEditor.GUID( guid2 ).ToHash( ), instanceId2 );
			
			return default;
		}
#endif
		
		public static class Editor
		{
			#if UNITY_EDITOR
			[UnityEditor.MenuItem("Tools/Flexy/AssetRefs/Enable Runtime Behavior")]			public static void		EnableRuntimeBehavior			( )	{ RuntimeBehaviorEnabled = true; }
			[UnityEditor.MenuItem("Tools/Flexy/AssetRefs/Disable Runtime Behavior")]		public static void		DisableRuntimeBehavior			( )	{ RuntimeBehaviorEnabled = false; }
			[UnityEditor.MenuItem("Tools/Flexy/AssetRefs/Enable Runtime Behavior", true)]	public static Boolean	EnableRuntimeBehaviorValidate	( )	{ return !RuntimeBehaviorEnabled; }
			[UnityEditor.MenuItem("Tools/Flexy/AssetRefs/Disable Runtime Behavior", true)]	public static Boolean	DisableRuntimeBehaviorValidate	( )	{ return RuntimeBehaviorEnabled; }
			#endif
		}
	}
}