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
#endif
	
	public static event		Action<Scene,Scene>?	NewSceneCreatedAndLoadingStarted; 
	
	public		 			UniTask<T?>				LoadAssetAsync<T>			( AssetRef @ref ) where T:Object		
	{
		if ( @ref.IsNone )
			return UniTask.FromResult<T?>( null );
		
		try
		{
#if UNITY_EDITOR
			if ( !RuntimeBehaviorEnabled || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode )
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
	public 					T?						LoadAssetSync<T>			( AssetRef @ref ) where T:Object		
	{
		if ( @ref.IsNone )
			return null;
		
		try
		{
#if UNITY_EDITOR
			if ( !RuntimeBehaviorEnabled || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode )
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
	public					String?					GetSceneName				( SceneRef @ref )						
	{
#if UNITY_EDITOR			
		if( !RuntimeBehaviorEnabled || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode )
		{
			var path			= UnityEditor.AssetDatabase.GUIDToAssetPath( @ref.Uid.ToGUID( ) );
			return System.IO.Path.GetFileNameWithoutExtension( path );
		}
#endif
		
		return GetSceneName_Impl( @ref );
	}
	public					LoadSceneTask			LoadSceneAsync				( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context )	
	{
		LoadSceneTask sceneTask;
		
#if UNITY_EDITOR			
		if( !RuntimeBehaviorEnabled || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode )
		{
			var path			= UnityEditor.AssetDatabase.GUIDToAssetPath( @ref.Uid.ToGUID( ) );
			var sceneLoadOp		= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new( p.LoadMode, p.PhysicsMode ) );
			sceneLoadOp.priority = p.Priority;
			sceneLoadOp.allowSceneActivation = p.ActivateOnLoad;
			var scene			= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );

			var info			= LoadSceneTask.RentSceneLoadData( );
			info.Scene			= scene;
			info.DelaySceneActivation = !p.ActivateOnLoad;
		
			sceneTask	= new( SceneLoadWaitImpl( sceneLoadOp, info ), info );
		}
		else
#endif
		{
			sceneTask	= LoadSceneAsync_Impl( @ref, p );
		}
		
		WaitSceneLoadStart( sceneTask, context ).Forget( );
		
		return sceneTask;
	}
	public					LoadSceneTask			LoadDummyScene				( GameObject ctx, LoadSceneMode mode, UnloadSceneOptions unloadOptions = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects )
	{
		var task = LoadDummyScene_Impl( mode, unloadOptions );
		
		WaitSceneLoadStart( task, ctx ).Forget( );
		
		return task;
	}
	
	public	static			T?						EditorLoadAsset<T>			( AssetRef<T> address ) where T : Object
	{
		var asset = EditorLoadAsset				( address, typeof(T) );
		return (T?)asset;
	}
	public	static			Object?					EditorLoadAsset				( AssetRef address, Type type )			
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
	public	static			AssetRef				EditorGetAssetAddress		( Object asset )						
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
	
	// Virtual interface for loding customisation
	protected abstract		UniTask<T?>				LoadAssetAsync_Impl<T>		( AssetRef @ref ) where T:Object;
	protected abstract		T?						LoadAssetSync_Impl<T>		( AssetRef @ref ) where T:Object;
	protected abstract		String?					GetSceneName_Impl			( SceneRef @ref );
	protected abstract 		LoadSceneTask			LoadSceneAsync_Impl			( SceneRef @ref, LoadSceneTask.Parameters p );
	protected virtual		LoadSceneTask			LoadDummyScene_Impl			( LoadSceneMode mode, UnloadSceneOptions unloadOptions = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects )
	{
		var data			= LoadSceneTask.RentSceneLoadData( );
		data.Scene			= default;
		
		return new( LoadDummyScene_Internal( data, mode, unloadOptions ), data );
		
		static async UniTask<Scene>  LoadDummyScene_Internal( LoadSceneTask.LoadData data, LoadSceneMode mode, UnloadSceneOptions unloadOptions )
		{
			try
			{
				AsyncOperation?	ao				= null;
				List<Scene>?	scenesToUnload	= null; 
					
				if( mode == LoadSceneMode.Single )
				{
					scenesToUnload = new( );
					var count = SceneManager.loadedSceneCount;
					for (var i = count - 1; i >= 0; i--)
						scenesToUnload.Add( SceneManager.GetSceneAt( i ) );
				}
			
				var dummy	= SceneManager.CreateScene( "Dummy" );
				data.Scene	= dummy;
					
				await UniTask.NextFrame( );
			
				SceneManager.SetActiveScene( dummy );
			
				var dummyCam = new GameObject( "DummyCam", typeof(Camera), typeof(AudioListener) );
					
				data.Progress = 0.9f;
					
				if( scenesToUnload != null )
				{
					foreach ( var scn in scenesToUnload )
					{
						if ( ao != null ) 
							await ao.ToUniTask( );
						
						ao = SceneManager.UnloadSceneAsync( scn, unloadOptions );
					}
				}
			
				if ( ao != null ) 
					await ao.ToUniTask( );	
				
				data.Progress = 1f;
				
				return dummy;
			}
			finally
			{
				data.Release( ).Forget( );
			}
		}
	}
	
	protected static async	UniTask<Scene>			SceneLoadWaitImpl			( AsyncOperation ao, LoadSceneTask.LoadData loadData )	
	{
		try
		{
			while ( !ao.isDone && ( ao.allowSceneActivation || !Mathf.Approximately( ao.progress, .9f ) ) )
			{
				await UniTask.NextFrame( );
				loadData.Progress = ao.progress;
			}
					
			loadData.Progress	= 1;
			
			if ( !ao.allowSceneActivation )
			{
				while ( loadData.DelaySceneActivation )
					await UniTask.NextFrame( );

				ao.allowSceneActivation = true;
				await UniTask.NextFrame( );
			}
			
			return loadData.Scene;
		}
		finally
		{
			loadData.Release( ).Forget( );
		}
	}
	protected static async	UniTask					WaitSceneLoadStart			( LoadSceneTask sceneTask, GameObject ctx )				
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

public static class LoadAssetTask
{
	public static	LoadAssetTask<T>		FromResult<T>	( T result )	=> new ( UniTask.FromResult( result ), null );
	
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

public readonly struct LoadAssetTask<T>
{
	public LoadAssetTask( UniTask<T> task, LoadAssetTask.LoadData? data )
	{
		_loadTask	= task;
		_loadData	= data;
	}

	private readonly UniTask<T>					_loadTask;
	private readonly LoadAssetTask.LoadData?	_loadData;
	
	public Single				Progress		=> IsDone ? 1 : _loadData?.Progress ?? 0;
	public Boolean				IsDone			=> _loadTask.Status != UniTaskStatus.Pending;
	public Boolean				IsSuccess		=> _loadTask.Status == UniTaskStatus.Succeeded;
	public UniTaskStatus		Status			=> _loadTask.Status;
	
	public T					GetResult		( ) => GetAwaiter( ).GetResult( );
	public UniTask<T>.Awaiter	GetAwaiter		( ) => _loadTask.GetAwaiter( );
	public void					Forget			( )	=> _loadTask.Forget( );
	public void					Cancel			( )	
	{
		if( _loadData != null )
			_loadData.IsCanceled = true;
	}
}
	
public readonly struct LoadSceneTask
{
	public LoadSceneTask( UniTask<Scene> t, LoadData data )
	{
		_loadTask	= t;
		_loadData	= data;
	}

	private readonly UniTask<Scene>		_loadTask;
	private readonly LoadData			_loadData;
	
	public Single			Progress	=> IsDone ? 1 : _loadData?.Progress ?? 0;
	public Scene			Scene		=> _loadData?.Scene ?? default;
	public Boolean			IsDone		=> _loadTask.Status != UniTaskStatus.Pending;
	public UniTaskStatus	Status		=> _loadTask.Status;
	
	public UniTask<Scene>.Awaiter	GetAwaiter				( ) => _loadTask.GetAwaiter( );
	public void						Forget					( )	=> _loadTask.Forget( );

	public async	UniTask<Scene>	WaitForSceneLoadStart	( )							
	{
		while ( !IsDone & _loadData.Scene == default )
			await UniTask.Yield( PlayerLoopTiming.LastPostLateUpdate );
		
		return _loadData.Scene;
	}
	public			UniTask			ContinueWith			( Action<Scene> action )	=> _loadTask.ContinueWith( action );
	public			void			AllowSceneActivation	( )							=> _loadData.DelaySceneActivation = false;
	
	public static LoadData RentSceneLoadData( ) => GenericPool<LoadData>.Get();
	
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
	
	public record struct Parameters
	( 
		LoadSceneMode		LoadMode		= LoadSceneMode.Additive, 
		LocalPhysicsMode	PhysicsMode		= LocalPhysicsMode.None, 
		Int32				Priority		= 100, 
		Boolean				ActivateOnLoad	= true 
	);
}