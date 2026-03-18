using System.Linq;
using Flexy.AssetRefs.Extra;

namespace Flexy.AssetRefs;

public abstract class SceneLoader
{
	public					String?					GetSceneName				( SceneRef @ref )						
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
	public					LoadSceneTask			LoadSceneAsync				( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context )	
	{
#if UNITY_EDITOR			
		if (!EditorBehaviourAndMenu.RuntimeBehaviorEnabled || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
		{
			var path			= UnityEditor.AssetDatabase.GUIDToAssetPath( @ref.Uid.ToGUID() );
			var sceneLoadOp		= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new(p.LoadMode, p.PhysicsMode) );
			
			var sceneTask = new LoadSceneTask(context, p, SceneManager.GetSceneAt( SceneManager.sceneCount - 1 ));

			return sceneTask.Run( SceneLoadWaitImpl(sceneLoadOp, sceneTask) );
		}
#endif
		return	LoadSceneAsync_Impl( @ref, p, context );
	}
	public					LoadSceneTask			LoadDummySceneAsync			( GameObject ctx, LoadSceneMode mode, UnloadSceneOptions unloadOptions = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects, DummySceneFlags dummyFlags = DummySceneFlags.DummyCamera | DummySceneFlags.DummyListener, Action? createSceneObjects = null )
	{
		var components = new List<Type>();

		if( (dummyFlags & DummySceneFlags.DummyCamera) != 0 )	components.Add( typeof(Camera) );
		if( (dummyFlags & DummySceneFlags.DummyListener) != 0 )	components.Add( typeof(AudioListener) );
	
		return LoadDummyScene_Impl( ctx, mode, unloadOptions, createSceneObjects, components.ToArray() );
	}
	public					LoadSceneTask			LoadDummySceneAsync			( GameObject ctx, LoadSceneMode mode, UnloadSceneOptions unloadOptions = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects, Action? createSceneObjects = null, params Type[] components )
	{
		return LoadDummyScene_Impl( ctx, mode, unloadOptions, createSceneObjects, components );
	}
	
	// Virtual interface for loding customisation
	protected abstract		String?					GetSceneName_Impl			( SceneRef @ref );
	protected abstract 		LoadSceneTask			LoadSceneAsync_Impl			( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context );
	protected virtual		LoadSceneTask			LoadDummyScene_Impl			( GameObject ctx, LoadSceneMode mode, UnloadSceneOptions unloadOptions, Action? createSceneObjects, params Type[] components )
	{
		var sceneTask			= new LoadSceneTask(ctx, new LoadSceneTask.Parameters(mode));

		return sceneTask.Run( LoadDummyScene_Internal( sceneTask, mode, unloadOptions, createSceneObjects, components ) );
		
		static async UniTask<Scene>  LoadDummyScene_Internal( LoadSceneTask sceneTask, LoadSceneMode mode, UnloadSceneOptions unloadOptions, Action? createSceneObjects, params Type[] components )
		{
			List<Scene>?	scenesToUnload	= null; 
				
			if (mode == LoadSceneMode.Single)
			{
				scenesToUnload = new();
				var count = SceneManager.loadedSceneCount;
				for (var i = count - 1; i >= 0; i--)
					scenesToUnload.Add( SceneManager.GetSceneAt(i) );
			}
		
			var dummySceneName = "Dummy";
			
			while(true)
			{
				var existingDummy = SceneManager.GetSceneByName(dummySceneName);
				if (!existingDummy.IsValid())
					break;
				
				dummySceneName += "2";
			}
		
			var dummy	= SceneManager.CreateScene(dummySceneName);
			sceneTask.Scene	= dummy;
			
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
		
			sceneTask.StepProgress = 0.9f;
				
			if (scenesToUnload != null)
			{
				foreach (var scn in scenesToUnload)
					if (scn.IsValid())
						await SceneManager.UnloadSceneAsync( scn, unloadOptions ).ToUniTask();
			}
		
			sceneTask.StepProgress = 1f;
			
			return dummy;
		}
	}
	
	protected static async	UniTask<Scene>			SceneLoadWaitImpl			( AsyncOperation ao, LoadSceneTask sceneTask )	
	{
		ao.allowSceneActivation	= sceneTask.Params.AllowActivation;
		ao.priority				= sceneTask.Params.Priority;
	
		while ( !ao.isDone && ( ao.allowSceneActivation || ao.progress < 0.9f ) )
		{
			await UniTask.NextFrame();
			sceneTask.StepProgress = ao.progress;
		}
				
		sceneTask.StepProgress = 0.9f;
		
		if (!ao.allowSceneActivation)
		{
			while (sceneTask.DelaySceneActivation)
				await UniTask.NextFrame();

			ao.allowSceneActivation = true;
			await UniTask.NextFrame( );
		}
		
		while (ao.progress < 1.0f)
		{
			sceneTask.StepProgress = ao.progress;
			await UniTask.NextFrame();
		}
		
		return sceneTask.Scene;
	}
}


public class LoadSceneTask : IProgress<Single>
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void StaticClear( ) => NewLoadSceneTaskStarted				= null;

	public LoadSceneTask( GameObject context, Parameters p, Scene scene = default, Single rangeMin = 0, Single rangeMax = 1, String? description = "Loading..." )
	{
		Context			= context;
		_params			= p;
		Scene			= scene;
		
		RangeMin		= rangeMin;
		RangeMax		= rangeMax;
		StepDescription	= description;
	}

	private UniTask<Scene>	ChainTask;
	private	Parameters		_params;
	
	public	GameObject		Context {get;}
	
	public	Scene			Scene;
	public	Single			StepProgress;
	
	public	Single			RangeMin;
	public	Single			RangeMax;
	public	String?			StepDescription;

	public	UniTask			CurrentStep {get; private set;}

	private List<Func<LoadSceneTask, UniTask>>?	LoadSteps;
	
	public	Single			Progress				=> IsDone ? 1 : (StepProgress - RangeMin) / (RangeMax - RangeMin);
	public	UniTaskStatus	Status					=> ChainTask.Status;
	public	Parameters		Params					=> _params;
	public	Boolean			DelaySceneActivation	=> !Params.AllowActivation;
	public	Boolean			IsDone					=> ChainTask.Status != UniTaskStatus.Pending;

	public static event		Action<LoadSceneTask>?	NewLoadSceneTaskStarted;
	
	public LoadSceneTask	Run						( UniTask<Scene> firstSceneTask )			
	{
		CurrentStep		= firstSceneTask;
		ChainTask		= LoadSceneStepsAsync();
		
		return this;
	}
	public	void 			AddLoadStep				( Func<LoadSceneTask, UniTask> loadStep )	
	{
		LoadSteps ??= new();
		LoadSteps.Add( loadStep );
	}
	
	public	UniTask			ContinueWith			( Action<Scene> action )=> ChainTask.ContinueWith(action);
	public	void			AllowSceneActivation	( )						=> _params.AllowActivation = true;
	
	public void						Report			( Single value )		=> StepProgress = value;
	public UniTask<Scene>.Awaiter	GetAwaiter		( ) => ChainTask.GetAwaiter();
	public void						Forget			( )	=> ChainTask.Forget();

	public			UniTask<Scene>	AsUniTask				( )	
	{
		return ChainTask;
	}
	public async	UniTask<Scene>	WaitForSceneLoadStart	( )	
	{
		while (!IsDone && Scene == default)
			await UniTask.Yield( PlayerLoopTiming.LastPostLateUpdate );
		
		return Scene;
	}
	private async	UniTask<Scene>	LoadSceneStepsAsync		( )	
	{
		await UniTask.NextFrame();
		await WaitForSceneLoadStart();
		
		try						{ NewLoadSceneTaskStarted?.Invoke( this );	}			
		catch( Exception ex )	{ Debug.LogException( ex );					}

		while (CurrentStep.Status == UniTaskStatus.Pending)
			await UniTask.NextFrame();
	
		if (Params.SetActive)
			SceneManager.SetActiveScene(Scene);
	
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