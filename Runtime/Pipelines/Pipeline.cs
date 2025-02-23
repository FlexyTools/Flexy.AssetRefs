namespace Flexy.AssetRefs.Pipelines
{
	[CreateAssetMenu(fileName = "Pipeline.ppl.asset", menuName = "Flexy/AssetRefs/Pipeline")]
	public class Pipeline : ScriptableObject
	{
		public Boolean			MasterEnabled;
        public EnabledTask[]	EnabledTasks	= {};

		public			void			RunTasks	( )					
		{
			if ( !MasterEnabled )
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
			if ( !MasterEnabled || EnabledTasks.Length <= 0 )
				return;

			try
			{
				for (var i = 0; i < EnabledTasks.Length; i++)
				{
					var et = EnabledTasks[i];
					if (!et.Enabled || et.Task == null)
						continue;

					#if UNITY_EDITOR
					UnityEditor.EditorUtility.DisplayProgressBar(name, UnityEditor.ObjectNames.NicifyVariableName( et.Task.GetType().Name ), i/(Single)EnabledTasks.Length);
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
	}
	
	public class Context : Dictionary<Type, System.Object>
	{
		public T		Get<T>	( )			where T : new()	=> TryGetValue(typeof(T), out var r) ? (T)r : (T)(this[typeof(T)] = new T());
		public void		Set<T>	( T obj )	where T : class	=> this[typeof(T)] = obj;
		public Boolean	Has<T>	( )			where T : new()	=> ContainsKey(typeof(T));
	} 
	
	public interface IPipelineTask
	{
		public void Run( Pipeline ppl, Context ctx );
	}
	
	#if UNITY_EDITOR
	[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs")]
	public class RunPipeline : IPipelineTask
	{
		[SerializeField]	Pipeline	_pipeline = null!;

		public void Run( Pipeline ppln, Context ctx ) => _pipeline.RunTasks(ctx);
	}
	#endif
}