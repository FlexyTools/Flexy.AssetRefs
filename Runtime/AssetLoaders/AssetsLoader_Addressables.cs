#if UNITY_ADDRESSABLES

using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.U2D;

namespace Flexy.AssetRefs.AssetLoaders;

public class AssetsLoader_Addressables : AssetsLoader
{
	public AssetsLoader_Addressables( AssetsLoader prevLoader, Addressables_RefsRemap remap )
	{
		_prevLoader	= prevLoader;
		_remap		= remap;
	}

	private readonly		AssetsLoader			_prevLoader;
	private readonly		Addressables_RefsRemap	_remap;	
	
	private					String					GetKey							( AssetRef @ref )		
	{
		return _remap.Get( @ref );
	}
	
	protected override		Int64					Package_GetDownloadBytes_Impl	( AssetRef @ref )		
	{
		var handle	= Addressables.GetDownloadSizeAsync( GetKey( @ref ) );
		var size	= handle.WaitForCompletion( );
		handle.Release( );
			
		return size;
	}
	protected override		LoadTask<Boolean>		Package_DownloadAsync_Impl		( AssetRef @ref )		
	{
		var handle	= Addressables.DownloadDependenciesAsync( GetKey( @ref ) );
		if( handle.IsDone )
			return LoadTask.FromResult( true );
					
		var info = GenericPool<LoadTask.ProgressInfo>.Get( );

		return new( Impl( handle, info ), info );
			
		static async UniTask<Boolean> Impl( AsyncOperationHandle h, LoadTask.ProgressInfo info )
		{
			try
			{
				while( !h.IsDone )
				{
					info.Progress = h.PercentComplete;
					await UniTask.DelayFrame( 1 );
				}
					
				return h.Status == AsyncOperationStatus.Succeeded;
			}
			finally
			{
				info.Release( ).Forget( );
				h.Release( );
			}
		}
	}
			
	protected override async UniTask<T>				LoadAssetAsync_Impl<T>			( AssetRef @ref ) 		
	{
		var key		= GetKey( @ref );
		var handle	= Addressables.LoadAssetAsync<Object>( key );
		var asset	= await handle.ToUniTask( );
		handle.Release( );
			
		if (asset)
			try
			{
				if( typeof(T).IsSubclassOf(typeof(MonoBehaviour)) )
					return  ((GameObject)asset).GetComponent<T>( );
				
				if( typeof(T) == typeof(Sprite) && asset is Texture2D tex )
					return (T)(Object)Sprite.Create( tex, new(0, 0, tex.width, tex.height), new( 0.5f, 0.5f ), 100, 0, SpriteMeshType.FullRect );
					
				return (T)asset;
			}
			catch( InvalidCastException ex ){ Debug.LogException( ex ); Debug.LogError( $"Asset: {asset.name} of type {asset.GetType().Name} can not be acsted to {typeof(T).Name}" );  }
			
		throw new ArgumentException( $"[AssetRef] - Resources Loader - Loading asset: {@ref} failed", "@ref" );
	}
	protected override		T						LoadAssetSync_Impl<T>			( AssetRef @ref ) 		
	{
		try
		{
			var handle = Addressables.LoadAssetAsync<Object>( GetKey( @ref ) );
			var asset = handle.WaitForCompletion( );
				
			//handle.Release( );
				
			if (asset) 
				try
				{
					if( typeof(T).IsSubclassOf(typeof(MonoBehaviour)) )
						return  ((GameObject)asset).GetComponent<T>( );
				
					if( typeof(T) == typeof(Sprite) && asset is Texture2D tex )
						return (T)(Object)Sprite.Create( tex, new(0, 0, tex.width, tex.height), new( 0.5f, 0.5f ), 100, 0, SpriteMeshType.FullRect );
					
					return (T)asset;
				}
				catch( InvalidCastException ex ){ Debug.LogException( ex ); Debug.LogError( $"Asset: {asset.name} of type {asset.GetType().Name} can not be acsted to {typeof(T).Name}" );  }
				
		}
		catch ( Exception ex ) { Debug.LogException( ex ); }
		{
			var asset = (T)EditorLoadAsset( @ref, typeof(T) )!;

			if (asset) 
				return asset;
		}
			
		throw new ArgumentException( $"[AssetRef] - Resources Loader - Loading asset: {@ref} failed", "@ref" );
	}
			
	protected override		String					GetSceneName_Impl				( SceneRef @ref )							
	{
		var locationsHandle	= Addressables.LoadResourceLocationsAsync( GetKey( @ref.Raw ) );
		var locations		= locationsHandle.WaitForCompletion( ); 
			
		foreach ( var location in locations )
		{
			if( !String.IsNullOrEmpty( location.InternalId ) )
				return location.InternalId;
		}
			
		return String.Empty;
	}
	protected override		SceneTask				LoadSceneAsync_Impl				( SceneRef @ref, SceneTask.Parameters p )	
	{
		var handle		= Addressables.LoadSceneAsync( GetKey( @ref.Raw ), new LoadSceneParameters( p.LoadMode, p.PhysicsMode ), p.ActivateOnLoad, p.Priority );
		var info		= SceneTask.GetSceneInfo( );
		info.DelaySceneActivation = !p.ActivateOnLoad;

		return new( Impl( handle, info ), info );
			
		static async UniTask<Scene> Impl ( AsyncOperationHandle h, LoadTask.SceneInfo info )
		{
			try
			{
				var op			= (AsyncOperationBase<SceneInstance>) h.GetType().GetField( "m_InternalOp", BindingFlags.Instance | BindingFlags.NonPublic ).GetValue( h );
				var field		= op.GetType().GetField( "m_Inst", BindingFlags.Instance | BindingFlags.NonPublic );
				var instance	= (SceneInstance)field.GetValue( op );
					
				while( instance.Equals( default(SceneInstance) ) )
				{
					await UniTask.DelayFrame( 1, PlayerLoopTiming.PreLateUpdate );
					instance	= (SceneInstance)field.GetValue( op );
				}
					
				info.Scene		= instance.Scene;
				
				while( !h.IsDone )
				{
					await UniTask.DelayFrame( 1 );
					info.Progress = h.PercentComplete;
				}
				
				info.Progress	= 1;
				await UniTask.DelayFrame( 1 );
				
				while( info.DelaySceneActivation )
					await UniTask.DelayFrame( 1 );

				instance.ActivateAsync( );
				
				return instance.Scene;
			}
			finally
			{
				info.Release( ).Forget( );
			}
		}
	}
}

public enum EDeliveryMode
{
	DoNotPackage = 0,
	InstallTime = 1,
	FastFollow = 2,
	OnDemand = 3
}

#if UNITY_EDITOR

public class AddressablesPopulateGroups : IRefsProcessor
{
	[SerializeField]	String			_groupName = null!;
	[SerializeField]	EDeliveryMode	_deliveryMode;
	
	public void Process( RefsCollector collector, RefsCollector.List refs, Boolean isPreview )
	{
		return;
		
		if( isPreview )
			return;
		
		var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;

		if (!settings) 
			return;
		
		var groupName = _groupName.Replace( '/', '-' ).Replace( '\\', '-' );
		var group = settings.FindGroup(groupName);
		if (!group)
			group = settings.CreateGroup(groupName, false, false, true, null, typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema), typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema));

		group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>().IncludeInBuild = true;
		
		foreach (var entry in group.entries.ToArray( ))
			group.RemoveAssetEntry( entry, false );	
			
		var paths = refs.Select( AssetDatabase.GetAssetPath ).Distinct( ).ToArray( );
		
		foreach (var path in paths)
		{
			var guid		= AssetDatabase.AssetPathToGUID(path);

			var e = settings.CreateOrMoveEntry(guid, group, false, false);
			var entriesAdded = new List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> {e};

			group		.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, false, true);
			settings	.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true, false);
		}
	}
	public void ProcessDisabled( RefsCollector collector, Boolean isPreview )
	{
		if( isPreview )
			return;
		
		var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;

		if (!settings) 
			return;
		
		var groupName = _groupName.Replace( '/', '-' ).Replace( '\\', '-' );
		var group = settings.FindGroup(groupName);
		if (group)
		{
			group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>().IncludeInBuild = false;
			
			foreach (var entry in group.entries.ToArray( ))
				group.RemoveAssetEntry( entry, false );	
		}
	}
}

public class AddressablesClearSubAssetRefsRemap : IRefsProcessor
{
	[SerializeField] Addressables_RefsRemap _remap = null!;
	
	public void Process( RefsCollector collector, RefsCollector.List refs, Boolean isPreview )
	{
		if( !_remap )
			return;
		
		_remap.Clear( );
	}
}

public class AddressablesPopulateSubAssetRefsRemap : IRefsProcessor
{
	[SerializeField] Addressables_RefsRemap _remap = null!;
	
	public void Process( RefsCollector collector, RefsCollector.List refs, Boolean isPreview )
	{
		if( !_remap )
			return;
		
		foreach ( var refObj in refs )
		{
			var @ref = AssetsLoader.EditorGetAssetAddress( refObj );
			if( @ref.SubId == 0 )
				continue;

			_remap.Add( @ref, $"{@ref.Uid}[{refObj.name}]" );
		}
	}
}

public class AddressablesPopulateSpriteAtlasRefsRemap : IRefsProcessor
{
	[SerializeField] Addressables_RefsRemap _remap = null!;
	
	public void Process( RefsCollector collector, RefsCollector.List refs, Boolean isPreview )
	{
		if( !_remap )
			return;
		
		foreach ( var @ref in refs.ToArray( ) )
		{
			if( @ref is not SpriteAtlas atlas )
				continue;

			var atlasRef = AssetsLoader.EditorGetAssetAddress( @ref );
			Debug.Log( $"Populate Refs Remap for atlas {atlas.name} - {atlasRef}" );

			foreach ( var obj in (Sprite[])typeof(UnityEditor.U2D.SpriteAtlasExtensions).GetMethod( "GetPackedSprites", BindingFlags.Static | BindingFlags.NonPublic ).Invoke( null, new []{ atlas } ) )
			{
				if( obj == null )
					continue;
				
				var assetRef = AssetsLoader.EditorGetAssetAddress( obj );
				refs.Remove( obj );
				_remap.Add( assetRef, $"{atlasRef.Uid}[{obj.name}]" );
				Debug.Log( $"Refs Remap: {assetRef} - {atlasRef.Uid}[{obj.name}]", obj );
			}
		}
	}
}

#endif

#if UNITY_EDITOR && FLEXY_ASSETREFS_DISABLE_ADDRESSABLES_INVASIVE_UI
[UnityEditor.InitializeOnLoad]
static class DisableInvasiveAddressablesAssetUI
{
	static DisableInvasiveAddressablesAssetUI( )
	{
		UnityEditor.Editor.finishedDefaultHeaderGUI += DisplayDgiId;
	}
		
	private static Action<UnityEditor.Editor>? _addressablesUIDelegate;
		
	static void DisplayDgiId(UnityEditor.Editor editor)
	{
		if( _addressablesUIDelegate == null )
		{
			var type	= Type.GetType( "UnityEditor.AddressableAssets.GUI.AddressableAssetInspectorGUI, Unity.Addressables.Editor" );
			var method	= type.GetMethod("OnPostHeaderGUI", BindingFlags.Static | BindingFlags.NonPublic);
			_addressablesUIDelegate = (Action<UnityEditor.Editor>)Delegate.CreateDelegate( typeof(Action<UnityEditor.Editor>), null, method );
		}
			
		UnityEditor.Editor.finishedDefaultHeaderGUI -= _addressablesUIDelegate;
	}
}
#endif

#endif