using Flexy.AssetRefs.Extra;

namespace Flexy.AssetRefs;

public abstract class AssetsLoader
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void StaticClear( )
	{
		NewSceneCreatedAndLoadingStarted = null;
	}
	
#if UNITY_EDITOR
	private static	Boolean?		_runtimeBehaviorEnabled;
	public static	Boolean			RuntimeBehaviorEnabled
	{
		get => _runtimeBehaviorEnabled ??= UnityEditor.EditorPrefs.GetBool( Application.productName + "=>Flexy/AssetRefs/RuntimeBehaviorEnabled" ); 
		set => UnityEditor.EditorPrefs.SetBool( Application.productName + "=>Flexy/AssetRefs/RuntimeBehaviorEnabled", (_runtimeBehaviorEnabled = value).Value );
	}
	private static	Boolean			AllowDirectAccessInEditor => !RuntimeBehaviorEnabled; 
#endif
	
	public static event	Action<Scene,Scene>?	NewSceneCreatedAndLoadingStarted; 
	
	public		 			UniTask<T?>			LoadAssetAsync<T>			( AssetRef @ref ) where T:Object		
	{
		if ( @ref.IsNone )
			return UniTask.FromResult<T?>( null );
		
		try
		{
#if UNITY_EDITOR
			if ( AllowDirectAccessInEditor || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode )
			{
				return EditorLoadAsync( @ref );
				static async UniTask<T?> EditorLoadAsync		( AssetRef @ref )
				{
					var asset = EditorLoadAsset( @ref, typeof(T) );
					
					await UniTask.NextFrame( PlayerLoopTiming.EarlyUpdate );
					
					return (T?)asset;
				}
			}
#endif	
			
			return LoadAssetAsync_Impl<T>( @ref );
		}
		catch( Exception ex )
		{
			Debug.LogException( ex );
			return UniTask.FromResult<T?>(null);
		}
	}
	public 					T?					LoadAssetSync<T>			( AssetRef @ref ) where T:Object		
	{
		if ( @ref.IsNone )
			return null;
		
		try
		{
#if UNITY_EDITOR
			if ( AllowDirectAccessInEditor || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode )
				return (T?)EditorLoadAsset( @ref, typeof(T) );
#endif
			
			return LoadAssetSync_Impl<T>( @ref );
		}
		catch( Exception ex )
		{
			Debug.LogException( ex );
			return null;
		}
	}
	public					String?				GetSceneName				( SceneRef @ref )						
	{
#if UNITY_EDITOR			
		if( AllowDirectAccessInEditor || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode )
		{
			var path			= UnityEditor.AssetDatabase.GUIDToAssetPath( @ref.Uid.ToGUID( ) );
			return System.IO.Path.GetFileNameWithoutExtension( path );
		}
#endif
		
		return GetSceneName_Impl( @ref );
	}
	public					SceneTask			LoadSceneAsync				( SceneRef @ref, SceneTask.Parameters p, GameObject context )	
	{
		SceneTask sceneTask;
		
#if UNITY_EDITOR			
		if( AllowDirectAccessInEditor || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode )
		{
			var path			= UnityEditor.AssetDatabase.GUIDToAssetPath( @ref.Uid.ToGUID( ) );
			var sceneLoadOp		= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new( p.LoadMode, p.PhysicsMode ) );
			sceneLoadOp.priority = p.Priority;
			sceneLoadOp.allowSceneActivation = p.ActivateOnLoad;
			var scene			= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );

			var info			= SceneTask.GetSceneData( );
			info.Scene			= scene;
			info.DelaySceneActivation = !p.ActivateOnLoad;
		
			sceneTask	= new( SceneLoadWaitImpl( sceneLoadOp, info ), info );
		}
		else
#endif
		{
			sceneTask	= LoadSceneAsync_Impl( @ref, p );
		}
		
		WaitSceneLoad( sceneTask, context ).Forget( );
		
		return sceneTask;
	}
	public					SceneTask			LoadDummyScene				( GameObject ctx, LoadSceneMode mode, UnloadSceneOptions unloadOptions = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects )
	{
		var task = LoadDummyScene_Impl( mode, unloadOptions );
		
		WaitSceneLoad( task, ctx ).Forget( );
		
		return task;
	}
	
	protected static async UniTask<Scene>		SceneLoadWaitImpl			( AsyncOperation ao, SceneTask.LoadData info )	
	{
		try
		{
			while ( !ao.isDone && ( ao.allowSceneActivation || !Mathf.Approximately( ao.progress, .9f ) ) )
			{
				await UniTask.DelayFrame( 1 );
				info.Progress = ao.progress;
			}
					
			info.Progress	= 1;
			await UniTask.DelayFrame( 1 );
			
			while ( !ao.allowSceneActivation && info.DelaySceneActivation )
				await UniTask.DelayFrame( 1 );

			ao.allowSceneActivation = true;
			
			return info.Scene;
		}
		finally
		{
			info.Release( ).Forget( );
		}
	}
	
	protected abstract		UniTask<T?>			LoadAssetAsync_Impl<T>		( AssetRef @ref ) where T:Object;
	protected abstract		T?					LoadAssetSync_Impl<T>		( AssetRef @ref ) where T:Object;
	protected abstract		String?				GetSceneName_Impl			( SceneRef @ref );
	protected abstract 		SceneTask			LoadSceneAsync_Impl			( SceneRef @ref, SceneTask.Parameters p );
	protected virtual		SceneTask			LoadDummyScene_Impl			( LoadSceneMode mode, UnloadSceneOptions unloadOptions = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects )
	{
		var data			= SceneTask.GetSceneData( );
		data.Scene			= default;
		
		return new( LoadDummyScene_Internal( data, mode, unloadOptions ), data );
		
		static async UniTask<Scene>  LoadDummyScene_Internal( SceneTask.LoadData data, LoadSceneMode mode, UnloadSceneOptions unloadOptions )
		{
		try
		{
			AsyncOperation? ao = null;
				
			List<Scene>? scenesToUnload = null; 
				
			if( mode == LoadSceneMode.Single )
			{
				scenesToUnload = new( );
				var count = SceneManager.loadedSceneCount;
				for (var i = count - 1; i >= 0; i--)
					scenesToUnload.Add( SceneManager.GetSceneAt( i ) );
				}
		
			var dummy = SceneManager.CreateScene( "Dummy" );
		
			data.Scene		= dummy;
				
			await UniTask.NextFrame( );
		
			SceneManager.SetActiveScene( dummy );
		
			var dummyCam = new GameObject( "DummyCam", typeof(Camera) );
				
			data.Progress = 0.9f;
				
			if( scenesToUnload != null )
			{
				foreach (var scn in scenesToUnload )
				{
					if (ao != null) await ao.ToUniTask( );
					ao = SceneManager.UnloadSceneAsync( scn, unloadOptions );
				}
			}
		
			if (ao != null) await ao.ToUniTask( );	
			
			data.Progress = 1f;
			
			return dummy;
		}
		finally
		{
			data.Release( ).Forget( );
	}
	}
	}
	
	public	static	T?				EditorLoadAsset<T>			( AssetRef<T> address ) where T : Object
	{
		var asset = EditorLoadAsset				( address, typeof(T) );
		return (T?)asset;
	}
	public	static	Object?			EditorLoadAsset				( AssetRef address, Type type )			
	{
#if UNITY_EDITOR
		
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
#endif
		
		return null;
	}
	public	static	AssetRef		EditorGetAssetAddress		( Object asset )						
	{
		if( !asset )
			return default;
		
#if UNITY_EDITOR
		
		if( UnityEditor.AssetDatabase.IsMainAsset( asset ) && UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid, out Int64 _ ) )
			return new( new UnityEditor.GUID( guid ).ToHash( ), 0 );	
		
		if( UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out long instanceId ) )
			return new( new UnityEditor.GUID( guid2 ).ToHash( ), instanceId );
		
#endif
		
		return default;
	}
	
	private static async	UniTask WaitSceneLoad ( SceneTask sceneTask, GameObject ctx )		
		{
			while( !sceneTask.IsDone && sceneTask.Scene == default )
			await UniTask.Yield( PlayerLoopTiming.LastPreLateUpdate );
			
			if( sceneTask.Scene != default )
			try						{ NewSceneCreatedAndLoadingStarted?.Invoke( ctx.scene, sceneTask.Scene );	}			
			catch( Exception ex )	{ Debug.LogException( ex );													}
		}
		
	public static class Editor
	{
#if UNITY_EDITOR
		public const String Menu = "Tools/Flexy/AssetRefs/AssetLoader/";
		[UnityEditor.MenuItem( Menu+"Enable Runtime Behavior", secondaryPriority = 101)]	static void		EnableRuntimeBehavior			( ) => RuntimeBehaviorEnabled = true;
		[UnityEditor.MenuItem( Menu+"Disable Runtime Behavior", secondaryPriority = 100)]	static void		DisableRuntimeBehavior			( ) => RuntimeBehaviorEnabled = false;
		[UnityEditor.MenuItem( Menu+"Enable Runtime Behavior", true)]						static Boolean	EnableRuntimeBehaviorValidate	( ) => !RuntimeBehaviorEnabled;
		[UnityEditor.MenuItem( Menu+"Disable Runtime Behavior", true)]						static Boolean	DisableRuntimeBehaviorValidate	( ) => RuntimeBehaviorEnabled;
#endif
	}
}
	
public readonly struct LoadTask<T>
{
	public LoadTask( UniTask<T> task, LoadTask.LoadData? data )
	{
		_loadTask	= task;
		_loadData	= data;
	}

	private readonly UniTask<T>				_loadTask;
	private readonly LoadTask.LoadData?		_loadData;
	
	public Single				Progress		=> IsDone ? 1 : _loadData?.Progress ?? 0;
	public Boolean				IsDone			=> _loadTask.Status != UniTaskStatus.Pending;
	public Boolean				IsSuccess		=> _loadTask.Status == UniTaskStatus.Succeeded;
	
	public T					GetResult		( ) => GetAwaiter( ).GetResult( );
	public UniTask<T>.Awaiter	GetAwaiter		( ) => _loadTask.GetAwaiter( );
	public void					Forget			( )	=> _loadTask.Forget( );
	public void					Cancel			( )	
	{
		if( _loadData != null )
			_loadData.IsCanceled = true;
	}
}
	
public readonly struct SceneTask
{
	public SceneTask( UniTask<Scene> t, LoadData info )
	{
		_loadTask	= t;
		_loadData		= info;
	}

	private readonly UniTask<Scene>		_loadTask;
	private readonly LoadData		_loadData;
	
	public Single			Progress	=> IsDone ? 1 : _loadData?.Progress ?? 0;
	public Scene			Scene		=> _loadData?.Scene ?? default;
	public Boolean			IsDone		=> _loadTask.Status != UniTaskStatus.Pending;
	public UniTaskStatus	Status		=> _loadTask.Status;
	
	public UniTask<Scene>.Awaiter	GetAwaiter	( ) => _loadTask.GetAwaiter( );
	public void						Forget		( )	=> _loadTask.Forget( );

	public async UniTask<Scene>		WaitForSceneLoadStart( )
	{
		while ( !IsDone & _loadData.Scene == default )
			await UniTask.Yield( PlayerLoopTiming.LastPostLateUpdate );
		
		return _loadData.Scene;
	}
	
	public static LoadData GetSceneData( ) => GenericPool<LoadData>.Get();
	
	public record struct Parameters( LoadSceneMode LoadMode = LoadSceneMode.Additive, LocalPhysicsMode PhysicsMode = LocalPhysicsMode.None, Int32 Priority = 100, Boolean ActivateOnLoad = true ) {};

	public UniTask	ContinueWith			( Action<Scene> action )	=> _loadTask.ContinueWith( action );
	public void		AllowSceneActivation	( )							=> _loadData.DelaySceneActivation = false;
	
	public class LoadData: IProgress<Single>
	{
		public Scene			Scene;
		public Single			Progress;
		public Boolean			DelaySceneActivation;
		
		public Boolean			IsDone => Progress >= 1.0;
		
		public async UniTask	Release			( )		
		{
			await UniTask.DelayFrame( 10 );

			Progress	= default;
			Scene		= default;
			DelaySceneActivation = default;
		
			GenericPool<LoadData>.Release( this );
		}
		public void				Report			( Single value ) => Progress = value;
	}
}
	
public static class LoadTask
{
	public static	LoadTask<T>		FromResult<T>	( T result )	=> new ( UniTask.FromResult( result ), null );
	
	public class LoadData: IProgress<Single>
	{
		public Single			Progress;
		public Boolean			IsCanceled;
		public Boolean			IsDone => Progress >= 1.0;
		
		public async UniTask	Release			( )		
		{
			await UniTask.DelayFrame( 10 );

			Progress	= default;
			IsCanceled	= default;
		
			GenericPool<LoadData>.Release( this );
		}
		public void				Report			( Single value ) => Progress = value;
	}
}