namespace Flexy.AssetRefs.Pipelines
{
	[CreateAssetMenu(fileName = "Pipeline.ppl.asset", menuName = "Flexy/AssetRefs/Pipeline")]
	public class Pipeline : ScriptableObject
	{
		public Boolean			DisablePipeline;
        public EnabledTask[]	EnabledTasks	= {};

		public			void			RunTasks	( )					
		{
			if ( DisablePipeline || EnabledTasks.Length <= 0 )
				return;
			
			var ctx = GenericPool<Context>.Get( );
			ctx.Clear( );
			
			try
			{
				RunTasks( ctx );
			}
			finally
			{
				ctx.Clear( );
				GenericPool<Context>.Release( ctx );
			}
		}
		public			void			RunTasks	( Context ctx )		
		{
			if ( DisablePipeline || EnabledTasks.Length <= 0 )
				return;

			try
			{
				for (var i = 0; i < EnabledTasks.Length; i++)
				{
					var et = EnabledTasks[i];
					if (!et.Enabled || et.Task == null)
						continue;

					#if UNITY_EDITOR
					Debug.Log($"{name}: Run task: {i+1}/{EnabledTasks.Length} - {UnityEditor.ObjectNames.NicifyVariableName( et.Task.GetInfo() )}");
					UnityEditor.EditorUtility.DisplayProgressBar(name, UnityEditor.ObjectNames.NicifyVariableName( et.Task.GetInfo() ), i/(Single)EnabledTasks.Length);
					#endif
					et.Task.Run(this, ctx);
				}
			}
			finally
			{
				#if UNITY_EDITOR
				UnityEditor.EditorUtility.ClearProgressBar( );
				#endif
			}
		}
		
		public T? GetTask<T>() where T:class, IPipelineTask	
		{
			foreach ( var et in EnabledTasks )
				if ( et.Task is T result )
					return result;
			
			return null;
		}

		[Serializable]
		public struct EnabledTask
		{
			public Boolean			Enabled;
			[SerializeReference]
			public IPipelineTask?	Task;
		}
		
#if UNITY_EDITOR
		// run it in CI with: "C:/Path/To/Unity.exe" -batchmode -quit -projectPath "D:/Path/To/Project" -executeMethod Flexy.AssetRefs.Pipelines.Pipeline.CmdRun -pipeline "PipelineName or GUID" 
		public static void CmdRun	( )		
		{
			var args = Environment.GetCommandLineArgs();
			var pipelineId = GetArg(args, "-pipeline");
        
			if (String.IsNullOrEmpty(pipelineId))
			{
				Debug.LogError("No pipeline specified. Use -pipeline argument with name or GUID.");
				UnityEditor.EditorApplication.Exit(1);
				return;
			}
        
			var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(pipelineId);
			if (String.IsNullOrEmpty(assetPath))
			{
				var guids = UnityEditor.AssetDatabase.FindAssets($"{pipelineId} t:Pipeline");
				if (guids.Length > 0)
					assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
			}
        
			var pipeline = UnityEditor.AssetDatabase.LoadAssetAtPath<Pipeline>(assetPath);
        
			if (pipeline == null)
			{
				Debug.LogError($"Pipeline not found: {pipelineId}");
				UnityEditor.EditorApplication.Exit(1);
				return;
			}
        
			Debug.Log($"Running pipeline: {assetPath}");
			pipeline.RunTasks();
			Debug.Log("Pipeline completed!");
			UnityEditor.EditorApplication.Exit(0);
			return;

			static String GetArg(String[] args, String name)
			{
				for (var i = 0; i < args.Length; i++)
					if (args[i] == name && i + 1 < args.Length)
						return args[i + 1];
						
				return String.Empty;
			}
		}
#endif
	}
	
	public class Context : Dictionary<Type, System.Object>
	{
		public T		Get<T>	( )			where T : new()	=> TryGetValue(typeof(T), out var r) ? (T)r : (T)(this[typeof(T)] = new T());
		public void		Set<T>	( T obj )	where T : class	=> this[typeof(T)] = obj;
		public Boolean	Has<T>	( )			where T : new()	=> ContainsKey(typeof(T));
	} 
	
	public interface IPipelineTask
	{
		public void		Run			( Pipeline ppl, Context ctx );
		public String	GetInfo		() => GetType().Name;
	}
	
	#if UNITY_EDITOR
	[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs")]
	public class RunPipeline : IPipelineTask
	{
		[SerializeField]	Pipeline	_pipeline = null!;

		public void		Run		( Pipeline ppln, Context ctx )	=> _pipeline.RunTasks(ctx);
		public String	GetInfo	( )								=> GetType().Name + ": " + _pipeline.name;
	}
	#endif
}