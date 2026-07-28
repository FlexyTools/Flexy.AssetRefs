using Flexy.AssetRefs.Extra;

namespace Flexy.AssetRefs;

public class SceneLoader
{
	public		String?				GetSceneName			( SceneRef @ref )						
	{
#if UNITY_EDITOR			
		if( !EditorBehaviourAndMenu.RuntimeBehaviorEnabled || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode )
		{
			var path			= UnityEditor.AssetDatabase.GUIDToAssetPath( @ref.Uid.ToGUID( ) );
			return System.IO.Path.GetFileNameWithoutExtension( path );
		}
#endif
		
		return GetSceneName_Impl( @ref );
	}
	public		LoadSceneTask		LoadSceneAsync			( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context )	
	{
#if UNITY_EDITOR			
		if (!EditorBehaviourAndMenu.RuntimeBehaviorEnabled || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
		{
			var path			= UnityEditor.AssetDatabase.GUIDToAssetPath( @ref.Uid.ToGUID() );
			var sceneLoadOp		= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new(p.LoadMode, p.PhysicsMode) );
			
			var scene		= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );
			var sceneTask	= new LoadSceneTask(context, @ref, p, scene);

			return sceneTask.Run( SceneLoadWaitImpl(sceneLoadOp, sceneTask) );
		}
#endif
		return	LoadSceneAsync_Impl( @ref, p, context );
	}
	public		LoadSceneTask		LoadDummySceneAsync		( GameObject ctx, LoadSceneMode mode, UnloadSceneOptions unloadOptions = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects, DummySceneFlags dummyFlags = DummySceneFlags.DummyCamera | DummySceneFlags.DummyListener, Action? createSceneObjects = null )
	{
		var components = new List<Type>();

		if( (dummyFlags & DummySceneFlags.DummyCamera) != 0 )	components.Add( typeof(Camera) );
		if( (dummyFlags & DummySceneFlags.DummyListener) != 0 )	components.Add( typeof(AudioListener) );
	
		return LoadDummyScene_Impl( ctx, mode, unloadOptions, createSceneObjects, components.ToArray() );
	}
	public		LoadSceneTask		LoadDummySceneAsync		( GameObject ctx, LoadSceneMode mode, UnloadSceneOptions unloadOptions = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects, Action? createSceneObjects = null, params Type[] components )
	{
		return LoadDummyScene_Impl( ctx, mode, unloadOptions, createSceneObjects, components );
	}
	
	// Virtual interface for loading customisation
	protected virtual		String?			GetSceneName_Impl	( SceneRef @ref )													=> ContentService.Ref.GetSceneName	(@ref);
	protected virtual		LoadSceneTask	LoadSceneAsync_Impl	( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context )	=> ContentService.Ref.LoadSceneAsync(@ref, p, context);
	protected virtual		LoadSceneTask	LoadDummyScene_Impl	( GameObject ctx, LoadSceneMode mode, UnloadSceneOptions unloadOptions, Action? createSceneObjects, params Type[] components ) => ContentService.Ref.LoadDummyScene(ctx, mode, unloadOptions, createSceneObjects, components); 
	
	protected static		UniTask<Scene>	SceneLoadWaitImpl	( AsyncOperation ao, LoadSceneTask sceneTask ) => ContentService.WaitSceneLoad(ao, sceneTask);
}


public class LoadSceneTask : IProgress<Single>
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void StaticClear( ) => NewLoadSceneTaskStarted	= null;

	[Obsolete( "Use overload with SceneRef instead", false )]
	public LoadSceneTask( GameObject context, Parameters p, Scene scene = default, Single rangeMin = 0, Single rangeMax = 1, String? description = "Loading..." ): this(context, default, p, scene, rangeMin, rangeMax, description) { }
	public LoadSceneTask( GameObject context, SceneRef sref, Parameters p, Scene scene = default, Single rangeMin = 0, Single rangeMax = 1, String? description = "Loading..." )
	{
		Context			= context;
		SceneRef		= sref;
		_params			= p;
		Scene			= scene;
		
		RangeMin		= rangeMin;
		RangeMax		= rangeMax;
		StepDescription	= description;
	}

	private UniTask<Scene>	_allSteps;
	private	Parameters		_params;
	private Scene			_scene;
	
	public	GameObject		Context		{get;}
	public	SceneRef		SceneRef	{get;}
	
	public	Scene			Scene		
	{
		get => _scene;
		set 
		{
			if (_scene != default || value == default)
				return;
				
			_scene = value;
			_scene.SetGuid(SceneRef.Uid.ToString());
			
			try						{ NewLoadSceneTaskStarted?.Invoke( this ); }			
			catch( Exception ex )	{ Debug.LogException( ex ); }			
		}
	}

	public	Single			StepProgress;
	public	Single			RangeMin;
	public	Single			RangeMax;
	public	String?			StepDescription;

	public	UniTask			CurrentStep {get; private set;}

	private List<Func<LoadSceneTask, UniTask>>?	LoadSteps;

	public	Single			Progress				=> IsDone ? 1 : (StepProgress - RangeMin) / (RangeMax - RangeMin);
	public	UniTaskStatus	Status					=> _allSteps.Status;
	public	Parameters		Params					=> _params;
	public	Boolean			DelaySceneActivation	=> !Params.AllowActivation;
	public	Boolean			IsDone					=> _allSteps.Status != UniTaskStatus.Pending;

	public static event		Action<LoadSceneTask>?	NewLoadSceneTaskStarted;
	
	public	LoadSceneTask	Run						( UniTask<Scene> firstSceneTask )			
	{
		CurrentStep		= firstSceneTask;
		_allSteps		= AllLoadSceneStepsAsync();
		
		return this;
	}
	public	void 			AddLoadStep				( Func<LoadSceneTask, UniTask> loadStep )	
	{
		LoadSteps ??= new();
		LoadSteps.Add( loadStep );
	}
	
	public	UniTask			ContinueWith			( Action<Scene> action )=> _allSteps.ContinueWith(action);
	public	void			AllowSceneActivation	( )						=> _params.AllowActivation = true;
	
	public	void					Report			( Single value )		=> StepProgress = value;
	public	UniTask<Scene>.Awaiter	GetAwaiter		( ) => _allSteps.GetAwaiter();
	public	void					Forget			( )	=> _allSteps.Forget();

	public			UniTask<Scene>	AsUniTask				( )	
	{
		return _allSteps;
	}
	public async	UniTask<Scene>	WaitForSceneLoadStart	( )	
	{
		while (Scene == default && !IsDone)
			await UniTask.Yield( PlayerLoopTiming.LastPostLateUpdate );
		
		return Scene;
	}
	private async	UniTask<Scene>	AllLoadSceneStepsAsync	( )	
	{
		while (CurrentStep.Status == UniTaskStatus.Pending)
			await UniTask.NextFrame();

		if (Params.SetActive && Scene.IsValid())
		{
			for (var i = 0; i < 3 && !Scene.isLoaded; i++)
				await UniTask.NextFrame();
			
			if (Scene.isLoaded)
				SceneManager.SetActiveScene(Scene);
		}
		
		while (LoadSteps != null && LoadSteps.Count > 0)
		{
			var step		= LoadSteps[0];
			LoadSteps.RemoveAt(0);
			CurrentStep		= step(this);
			
			while (CurrentStep.Status == UniTaskStatus.Pending)
				await UniTask.NextFrame();
		}
		
		return Scene;
	}
	
	public record struct Parameters
	( 
		LoadSceneMode		LoadMode		= LoadSceneMode.Additive, 
		LocalPhysicsMode	PhysicsMode		= LocalPhysicsMode.None, 
		Int32				Priority		= 100, 
		Boolean				AllowActivation	= true,  
		Boolean				SetActive		= false
	);
}

[Flags]
public enum DummySceneFlags: Byte
{
	None = 0,
	DummyCamera = 1 << 0,
	DummyListener = 1 << 1,
}