using System.Linq;
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
		get => _runtimeBehaviorEnabled ??= UnityEditor.EditorPrefs.GetBool( Application.productName + "=>Fun.Flexy/AssetRefs/RuntimeBehaviorEnabled" ); 
		set => UnityEditor.EditorPrefs.SetBool( Application.productName + "=>Fun.Flexy/AssetRefs/RuntimeBehaviorEnabled", (_runtimeBehaviorEnabled = value).Value );
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
			if (!RuntimeBehaviorEnabled || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return EditorLoadAsync(@ref);
				
				static async UniTask<T?> EditorLoadAsync		( AssetRef @ref )
				{
					var asset = EditorLoadAsset(new AssetRef<T>(@ref.Uid, @ref.SubId));
					await UniTask.NextFrame(PlayerLoopTiming.EarlyUpdate);
					return asset;
				}
			}
#endif	
			
			return LoadAssetAsync_Impl<T>(@ref);
		}
		catch( Exception ex )
		{
			Debug.LogException(ex);
			return UniTask.FromResult<T?>(null);
		}
	}
	public 					T?						LoadAssetSync<T>			( AssetRef @ref ) where T:Object		
	{
		if (@ref.IsNone)
			return null;
		
		try
		{
#if UNITY_EDITOR
			if (!RuntimeBehaviorEnabled || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
				return EditorLoadAsset(new AssetRef<T>(@ref.Uid, @ref.SubId));
#endif
			
			return LoadAssetSync_Impl<T>(@ref);
		}
		catch( Exception ex )
		{
			Debug.LogException(ex);
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
			var loadData		= LoadSceneTask.RentSceneLoadData();
			loadData.Scene		= scene;
			loadData.DelaySceneActivation = !p.ActivateOnLoad;
		
			return loadData.RunChain( SceneLoadWaitImpl(sceneLoadOp, loadData) );
		}
		else
#endif
		{
			sceneTask	= LoadSceneAsync_Impl( @ref, p );
		}
		
		WaitSceneLoadStart( sceneTask, context ).Forget( );
		
		return sceneTask;
	}
	public					LoadSceneTask			LoadDummyScene				( GameObject ctx, LoadSceneMode mode, UnloadSceneOptions unloadOptions = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects, DummySceneFlags dummyFlags = DummySceneFlags.DummyCamera | DummySceneFlags.DummyListener, Action? createSceneObjects = null )
	{
		var task = LoadDummyScene_Impl( mode, unloadOptions, dummyFlags, createSceneObjects );
		
		WaitSceneLoadStart( task, ctx ).Forget( );
		
		return task;
	}
	public					LoadSceneTask			LoadDummyScene				( GameObject ctx, LoadSceneMode mode, params Type[] components )	
	{
		return LoadDummyScene( ctx, mode, UnloadSceneOptions.None, null, components );
	}
	public					LoadSceneTask			LoadDummyScene				( GameObject ctx, LoadSceneMode mode, UnloadSceneOptions unloadOptions = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects, Action? createSceneObjects = null, params Type[] components )
	{
		var task = LoadDummyScene_Impl( mode, unloadOptions, createSceneObjects, components );
		
		WaitSceneLoadStart( task, ctx ).Forget( );
		
		return task;
	}
	
	public	static			T?						EditorLoadAsset<T>			( AssetRef<T> address ) where T : Object
	{
		var asset = EditorLoadAssetRaw	(address);
		
		if (asset is GameObject go && typeof(T).IsSubclassOf(typeof(Component)))
			return go.GetComponent<T>();
		
		return (T?)asset;
	}
	public	static			Object?					EditorLoadAsset				( AssetRef address, Type _ ) => EditorLoadAssetRaw(address);
	public	static			Object?					EditorLoadAssetRaw			( AssetRef address )			
	{
#if UNITY_EDITOR
		
		if (address.IsNone)
			return null;

		if (address.SubId == 0) //pure guid
		{
			var path = UnityEditor.AssetDatabase.GUIDToAssetPath( address.Uid.ToGUID() );
		
			return UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
		}
		else
		{
			var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( address.Uid.ToGUID() );
			
			foreach ( var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path) )
			{
				if (!asset || !UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out Int64 instanceId )) 
					continue;
				
				if (address.SubId == instanceId)
					return asset;
			}
		}
#endif
		
		return null;
	}
	public	static			AssetRef				EditorGetAssetAddress		( Object asset )				
	{
		if (!asset)
			return default;
		
#if UNITY_EDITOR
		
		if ((asset is Component or GameObject or ScriptableObject || UnityEditor.AssetDatabase.IsMainAsset(asset)) && UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid, out Int64 _ ))
			return new( new UnityEditor.GUID(guid).ToHash(), 0 );	
		
		if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out long instanceId ))
			return new( new UnityEditor.GUID(guid2).ToHash(), instanceId );
		
#endif
		
		return default;
	}
	
	// Virtual interface for loding customisation
	protected abstract		UniTask<T?>				LoadAssetAsync_Impl<T>		( AssetRef @ref ) where T:Object;
	protected abstract		T?						LoadAssetSync_Impl<T>		( AssetRef @ref ) where T:Object;
	protected abstract		String?					GetSceneName_Impl			( SceneRef @ref );
	protected abstract 		LoadSceneTask			LoadSceneAsync_Impl			( SceneRef @ref, LoadSceneTask.Parameters p );
	protected virtual		LoadSceneTask			LoadDummyScene_Impl			( LoadSceneMode mode, UnloadSceneOptions unloadOptions, DummySceneFlags dummyFlags, Action? createSceneObjects )
	{
		var components = new List<Type>();

		if( (dummyFlags & DummySceneFlags.DummyCamera) != 0 )	components.Add( typeof(Camera) );
		if( (dummyFlags & DummySceneFlags.DummyListener) != 0 )	components.Add( typeof(AudioListener) );
	
		return LoadDummyScene_Impl( mode, unloadOptions, createSceneObjects, components.ToArray() ); 
	}
	protected virtual		LoadSceneTask			LoadDummyScene_Impl			( LoadSceneMode mode, UnloadSceneOptions unloadOptions, Action? createSceneObjects, params Type[] components )
	{
		var data			= LoadSceneTask.RentSceneLoadData();
		data.Scene			= default;
		
		return data.RunChain( LoadDummyScene_Internal( data, mode, unloadOptions, createSceneObjects, components ) );
		
		static async UniTask<Scene>  LoadDummyScene_Internal( LoadSceneTask.LoadData loadData, LoadSceneMode mode, UnloadSceneOptions unloadOptions, Action? createSceneObjects, params Type[] components )
		{
			List<Scene>?	scenesToUnload	= null; 
				
			if (mode == LoadSceneMode.Single)
			{
				scenesToUnload = new();
				var count = SceneManager.loadedSceneCount;
				for (var i = count - 1; i >= 0; i--)
					scenesToUnload.Add( SceneManager.GetSceneAt(i) );
			}
		
			var dummy	= SceneManager.CreateScene("Dummy");
			loadData.Scene	= dummy;
			
			await UniTask.NextFrame();
		
			SceneManager.SetActiveScene(dummy);
		
			var dummyObj = new GameObject( "DummyObj", components );
		
			#if UNITY_URP
			var hasCam = components.Contains(typeof(Camera));
			if (hasCam)
				dummyObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
			#elif UNITY_HDRP
			var hasCam = components.Contains(typeof(Camera));
			if (hasCam)
				dummyObj.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
			#endif
		
			if ( createSceneObjects != null)
			{
				try						{ createSceneObjects(); }
				catch (Exception ex)	{ Debug.LogException(ex); }
			}
		
			loadData.Progress = 0.9f;
				
			if (scenesToUnload != null)
			{
				foreach (var scn in scenesToUnload)
					if (scn.IsValid())
						await SceneManager.UnloadSceneAsync( scn, unloadOptions ).ToUniTask();
			}
		
			loadData.Progress = 1f;
			
			return dummy;
		}
	}
	
	protected static async	UniTask<Scene>			SceneLoadWaitImpl			( AsyncOperation ao, LoadSceneTask.LoadData loadData )	
	{
		while ( !ao.isDone && ( ao.allowSceneActivation || ao.progress < 0.9f ) )
		{
			await UniTask.NextFrame();
			loadData.Progress = ao.progress;
		}
				
		loadData.Progress = 0.9f;
		
		if (!ao.allowSceneActivation)
		{
			while ( loadData.DelaySceneActivation )
				await UniTask.NextFrame( );

			ao.allowSceneActivation = true;
			await UniTask.NextFrame( );
		}
		
		while (ao.progress < 1.0f)
		{
			loadData.Progress = ao.progress;
			await UniTask.NextFrame( );
		}
		
		return loadData.Scene;
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
		public const String Menu = "Tools/Fun.Flexy/Asset Refs/Asset Loader/";
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
	public LoadSceneTask( LoadData data ) => _loadData	= data;
	public LoadSceneTask( UniTask<Scene> sceneTask, LoadData data )
	{
		_loadData = data;
		data.RunChain(sceneTask);
	}
	
	private readonly LoadData	_loadData;
	
	public Single			Progress	=> IsDone ? 1 : _loadData?.Progress ?? 0;
	public Scene			Scene		=> _loadData?.Scene ?? default;
	public Boolean			IsDone		=> _loadData.ChainTask.Status != UniTaskStatus.Pending;
	public UniTaskStatus	Status		=> _loadData.ChainTask.Status;
	
	public UniTask<Scene>.Awaiter	GetAwaiter				( ) => _loadData.ChainTask.GetAwaiter();
	public void						Forget					( )	=> _loadData.ChainTask.Forget();

	public async	UniTask<Scene>	WaitForSceneLoadStart	( )									
	{
		while ( !IsDone & _loadData.Scene == default )
			await UniTask.Yield( PlayerLoopTiming.LastPostLateUpdate );
		
		return _loadData.Scene;
	}
	public			UniTask			ContinueWith			( Action<Scene> action )			=> _loadData.ChainTask.ContinueWith(action);
	public			void 			SetNextStep				( Func<LoadData, UniTask> nextStep )=> _loadData.NextStep = nextStep;
	public			void			AllowSceneActivation	( )									=> _loadData.DelaySceneActivation = false;
	
	public static LoadData RentSceneLoadData( ) => GenericPool<LoadData>.Get();
	
	public class LoadData: IProgress<Single>
	{
		internal UniTask<Scene>	ChainTask;
		public UniTask			CurrentStep {get; private set;}
	
		public Scene			Scene;
		public Single			Progress;
		public Boolean			DelaySceneActivation;
		
		public Single			RangeMin = 0;
		public Single			RangeMax = 1;
		public String?			StepDescription;
		
		public Func<LoadData, UniTask>?	NextStep;
		
		public Single			ProgressOverall	=> (Progress - RangeMin) / (RangeMax - RangeMin);
		public Boolean			IsDone			=> ChainTask.Status != UniTaskStatus.Pending;
		
		public LoadSceneTask		RunChain		( UniTask<Scene> loadTask, Single rangeMin = 0, Single rangeMax = 1 )	
		{
			RangeMin	= rangeMin;
			RangeMax	= rangeMax;
			CurrentStep	= loadTask;
			ChainTask	= WaitLoadChain(this);
			
			return new(this);
		}
		public void					Report			( Single value )		=> Progress = value;
		static async UniTask<Scene>	WaitLoadChain	( LoadData loadData )	
		{
			try
			{
				while (loadData.CurrentStep.Status == UniTaskStatus.Pending)
					await UniTask.NextFrame();
		
				while (loadData.NextStep != null)
				{
					var nextStep			= loadData.NextStep;
					loadData.NextStep		= null;
					loadData.CurrentStep	= nextStep(loadData);
					
					while (loadData.CurrentStep.Status == UniTaskStatus.Pending)
						await UniTask.NextFrame();
				}
			}
			finally
			{
				Release(loadData).Forget();
				
				static async UniTask	Release			(  LoadData loadData )		
				{
					await UniTask.DelayFrame(10);

					loadData.ChainTask		= default;
					loadData.CurrentStep	= default;
					loadData.Scene			= default;
					loadData.Progress		= default;
					loadData.RangeMin		= default;
					loadData.RangeMax		= default;
					loadData.StepDescription = default;
					loadData.NextStep	= default;
					loadData.DelaySceneActivation = default;
		
					GenericPool<LoadData>.Release( loadData );
				}
			}
		
			return loadData.Scene;
		}
	}
	
	public record struct Parameters
	( 
		LoadSceneMode		LoadMode		= LoadSceneMode.Additive, 
		LocalPhysicsMode	PhysicsMode		= LocalPhysicsMode.None, 
		Int32				Priority		= 100, 
		Boolean				ActivateOnLoad	= true 
	);
}

[Flags]
public enum DummySceneFlags: Byte
{
	None = 0,
	DummyCamera = 1 << 0,
	DummyListener = 1 << 1,
}