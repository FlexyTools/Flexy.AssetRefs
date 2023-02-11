using System;
using Cysharp.Threading.Tasks;
using Flexy.JsonXSpace;
using UnityEngine;
using Object = UnityEngine.Object;
//using Hash128 = UnityEngine.Hash128;

namespace Flexy.AssetRefs 
{
	[Serializable]
	public struct AssetRef<T> : ISerializeAsString where T: Object
	{
		public	AssetRef ( String refAddress )	
		{
			_refAddress = refAddress;
		}
		
		[SerializeField] String			_refAddress; //asset guid optionally with subasset id
		// [SerializeField] Hash128		_refAddress;
		// [SerializeField] Int64			_internalId;
		
		public			Boolean			IsNone				=> String.IsNullOrEmpty( _refAddress );
		
		public	async	UniTask			DownloadDependencies( IProgress<Single> progress = null )	
		{
			var resolver	= AssetRef.GetAssetResolver( );
			await resolver.DownloadDependencies( _refAddress, progress );
		}
		public	async	UniTask<Int32>	GetDownloadSize		( )										
		{
			var resolver	= AssetRef.GetAssetResolver( );
			return await resolver.GetDownloadSize( _refAddress );
		}
		
		public			T				LoadAssetSync		( )										
		{
			var resolver	= AssetRef.GetAssetResolver( );
			var asset		= resolver.LoadAssetSync<T>( _refAddress );
			
			if( asset is T tr )
				return tr;
			
			if ( TryTypedOf( asset, out T result ) )
				return result;
			
			return default;
		}
		
		public async	UniTask<T> 		LoadAssetAsync		( IProgress<Single> progress = null )	
		{
			var resolver	= AssetRef.GetAssetResolver( );
			var asset		= await resolver.LoadAssetAsync<T>( _refAddress, progress );
			
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

		public override	String	ToString	( )				
		{
			return _refAddress;
		}
		public			void	FromString	( String data )	
		{
			#if UNITY_EDITOR
			if( data.Length >= 4 && data[3] == ':' )
				data = data[4..];
			#endif
			
			_refAddress = data;
		}
	}

	
	public static class AssetRef
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init( )
		{
			_sceneResolver		= new SceneResolver( );
			_assetResolver		= new AssetResolver( );
		}
	
		public const	Boolean		IsEditor = 
			#if UNITY_EDITOR
			true;
			#else
			false;
			#endif
		
		public		static			Boolean				RuntimeBehaviorEnabled;
		public		static			Boolean				AllowDirectAccessInEditor => IsEditor && !RuntimeBehaviorEnabled;
		
		private		static			SceneResolver		_sceneResolver;
		private		static			AssetResolver		_assetResolver;
		
		public		static			SceneResolver		GetSceneResolver	( )		
		{
			if( _sceneResolver == null )
				Editor.RegisterResolversInEditor( );
				
			return _sceneResolver;
		}
		public		static			AssetResolver		GetAssetResolver	( )		
		{
			if( _assetResolver == null )
				Editor.RegisterResolversInEditor( );
				
			return _assetResolver;
		}
		
		public static class Editor
		{
			internal static void RegisterResolversInEditor()
			{
				#if UNITY_EDITOR
				_sceneResolver = new SceneResolver( );
				_assetResolver = new AssetResolver( );
				#endif
			}
			
			#if UNITY_EDITOR
			[UnityEditor.MenuItem("Tools/Flexy/AssetRefs/Enable Runtime Behavior")]			public static void		EnableRuntimeBehavior			( )	{ RuntimeBehaviorEnabled = true; }
			[UnityEditor.MenuItem("Tools/Flexy/AssetRefs/Disable Runtime Behavior")]		public static void		DisableRuntimeBehavior			( )	{ RuntimeBehaviorEnabled = false; }
			[UnityEditor.MenuItem("Tools/Flexy/AssetRefs/Enable Runtime Behavior", true)]	public static Boolean	EnableRuntimeBehaviorValidate	( )	{ return !RuntimeBehaviorEnabled; }
			[UnityEditor.MenuItem("Tools/Flexy/AssetRefs/Disable Runtime Behavior", true)]	public static Boolean	DisableRuntimeBehaviorValidate	( )	{ return RuntimeBehaviorEnabled; }
			#endif
		}
	}
}