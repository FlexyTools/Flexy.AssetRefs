using Flexy.Core;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Flexy.AssetRefs
{
	public abstract class AssetsLoader
	{
		public				UniTask<UInt64>			Package_GetDownloadSize		( AssetRef @ref ) => Package_GetDownloadSize_Impl	( @ref );
		public				AsyncOperation			Package_DownloadAsync		( AssetRef @ref ) => Package_DownloadAsync_Impl		( @ref );
		
		public async 		UniTask<T>				LoadAssetAsync<T>			( AssetRef @ref ) where T:Object		
		{
#if UNITY_EDITOR
			if ( AssetRef.AllowDirectAccessInEditor )
			{
				await UniTask.DelayFrame(2);
				return (T)AssetRef.EditorLoadAsset( @ref, typeof(T) );
			}
#endif
			return await LoadAssetAsync_Impl<T>( @ref );
		}
		public 				T						LoadAssetSync<T>			( AssetRef @ref ) where T:Object		
		{
#if UNITY_EDITOR
			if ( AssetRef.AllowDirectAccessInEditor )
				return (T)AssetRef.EditorLoadAsset( @ref, typeof(T) );
#endif
			
			return LoadAssetSync_Impl<T>( @ref );
		}
		public				LoadTask<Scene>			LoadSceneAsync				( SceneRef @ref, LoadSceneMode loadMode, GameObject context )	
		{
			var op = GenericPool<LoadTaskData>.Get( );
			
#if UNITY_EDITOR			
			if( AssetRef.AllowDirectAccessInEditor )
				return new ( LoadSceneInEditorAsync( this, context, @ref, loadMode, op ), op );
#endif
			return new( LoadSceneInternalAsync( this, context, @ref, loadMode, op ), op );
			
#if UNITY_EDITOR
			static async UniTask<Scene> LoadSceneInEditorAsync( AssetsLoader loader, GameObject context, SceneRef @ref, LoadSceneMode loadMode, LoadTaskData op )
			{
				// try load scene normally and only if failed load throug editor routime
				try						{ return await  LoadSceneInternalAsync( loader, context, @ref, loadMode, op ); }
				catch ( Exception ex )	{ Utils.Logger.Debug.LogException( ex ); }
				
				try
				{
					op.Percentages	= new Vector3( 0, 0, 1 );
					op.LoadingState = ELoadingState.Load;
					
					var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( Utils.Editor.Hash128EditorGUIDExt.ToGUID(@ref.Uid) );
					var sceneLoadOp	= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new LoadSceneParameters { loadSceneMode = loadMode } );
					
					var scene		= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );

					GameContext.GetCtx( context.scene ).RegisterGameScene( scene );
	
					while( !sceneLoadOp.isDone )
					{
						op.StateProgress	= sceneLoadOp.progress;
						await UniTask.DelayFrame( 1 );
					}
					
					return scene;
				}
				finally
				{
					op.Release( ).Forget( Debug.LogException );
				}
			}
#endif
			
			static async UniTask<Scene> LoadSceneInternalAsync( AssetsLoader loader, GameObject context, SceneRef @ref, LoadSceneMode loadMode, LoadTaskData op )
			{
				try
				{
					if( !loader.Package_IsDownloaded_Impl( (AssetRef)@ref ) )
					{
						op.Percentages	= new Vector3( 0.4f, 0.5f, 1 );
						op.LoadingState = ELoadingState.Download;
						
						var sizeInBytes	= await loader.Package_GetDownloadSize_Impl( (AssetRef)@ref );
						var downloadOp	= loader.Package_DownloadAsync_Impl( (AssetRef)@ref );
						
						while( !downloadOp.isDone )
						{
							op.StateProgress	= downloadOp.progress;
							op.BytesDownloaded	= (UInt64)( downloadOp.progress * sizeInBytes );
							await UniTask.DelayFrame( 1 );
						}
					}
					else
					{
						op.Percentages	= new Vector3( 0, 0, 1 );
					}
					
					op.LoadingState = ELoadingState.Load;
					
					var sceneLoadOp		= loader.LoadSceneAsync_Impl( @ref, loadMode );
					var scene			= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );
					GameContext.GetCtx( context.scene ).RegisterGameScene( scene );

					op.Operation = sceneLoadOp;
					
					while( !sceneLoadOp.isDone )
					{
						op.StateProgress	= sceneLoadOp.progress;
						await UniTask.DelayFrame( 1 );
					}
					
					return scene;
				}
				finally
				{
					op.Release( ).Forget( Debug.LogException );
				}
			}
		}
		public 				LoadTask<T>				LoadSceneAsync<T>			( SceneRef @ref, LoadSceneMode loadMode, GameObject context ) where T: MonoBehaviour	
		{
			var sceneLoadTask	= LoadSceneAsync( @ref, loadMode, context );
			var result			= LoadTask<T>.FromAnotherLoadTask ( LoadSceneBehavior( sceneLoadTask.Task ), sceneLoadTask );
			
			return result;
			
			static async UniTask<T> LoadSceneBehavior( UniTask<Scene> sceneLoader )
			{
				var scene = await sceneLoader;
				
				foreach ( var go in scene.GetRootGameObjects( ) )
				{
					if ( !go.TryGetComponent<T>( out var component ) )
						continue;
		
					return component;
				}
			
				return null;	
			}
		}
		public				LoadTask<Scene>			LoadSceneByNameAsync		( String sceneName, LoadSceneMode loadMode, GameObject context )
		{
			var op = GenericPool<LoadTaskData>.Get( );
			
			return new( LoadSceneInternalAsync( this, context, sceneName, loadMode, op ), op );
			
			static async UniTask<Scene> LoadSceneInternalAsync( AssetsLoader loader, GameObject context, String sceneName, LoadSceneMode loadMode, LoadTaskData op )
			{
				try
				{
					op.Percentages		= new Vector3( 0, 0, 1 );
					op.LoadingState		= ELoadingState.Load;
					
					var sceneLoadOp		= SceneManager.LoadSceneAsync( sceneName, new LoadSceneParameters{ loadSceneMode = loadMode} );
					var scene			= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );
					GameContext.GetCtx( context.scene ).RegisterGameScene( scene );

					op.Operation = sceneLoadOp;
					
					while( !sceneLoadOp.isDone )
					{
						op.StateProgress	= sceneLoadOp.progress;
						await UniTask.DelayFrame( 1 );
					}
					
					return scene;
				}
				finally
				{
					op.Release( ).Forget( Debug.LogException );
				}
			}
		}
		
		protected abstract	Boolean					Package_IsDownloaded_Impl	( AssetRef @ref );
		protected abstract	UniTask<UInt64>			Package_GetDownloadSize_Impl( AssetRef @ref );
		protected abstract 	AsyncOperation			Package_DownloadAsync_Impl	( AssetRef @ref );
		
		protected abstract	UniTask<T>				LoadAssetAsync_Impl<T>		( AssetRef @ref ) where T:Object;
		protected abstract	T						LoadAssetSync_Impl<T>		( AssetRef @ref ) where T:Object;
		protected abstract 	AsyncOperation			LoadSceneAsync_Impl			( SceneRef @ref, LoadSceneMode loadMode );
	}
	
	public struct LoadTask<T>
	{
		internal LoadTask( UniTask<T> t, LoadTaskData o)
		{
			_overallTask	= t;
			_operation		= o;
		}
		public static LoadTask<T> FromAnotherLoadTask<T2> ( UniTask<T> t, LoadTask<T2> lt2 )
		{
			return new LoadTask<T>
			{
				_overallTask = t,
				_operation = lt2._operation
			};
		}

		private UniTask<T>		_overallTask;
		private LoadTaskData	_operation;
		
		public Boolean			IsEmpty			=> _operation == null;
		
		public ELoadingState	LoadingState	=> _operation?.LoadingState ?? ELoadingState.None;
		public Single			StateProgress	=> _operation?.StateProgress ?? 0;
		public Single			OverallProgress	=> _operation?.OverallProgress ?? 0;
		public UInt64			BytesDownloaded	=> _operation?.BytesDownloaded ?? 0;
		
		public UniTask<T>		Task			=> _overallTask;
		public Boolean			IsDone			=> _overallTask.Status != UniTaskStatus.Pending;
		
		public UniTask			WaitFrame		( ) => UniTask.DelayFrame( 1 );
		public T				Result			( ) => GetAwaiter( ).GetResult( );
		public UniTask			WaitState		( ELoadingState state ) { var o = _operation; return UniTask.WaitWhile( () => o?.LoadingState < state ); }
		
		public UniTask<T>.Awaiter	GetAwaiter	( )	
		{
			return _overallTask.GetAwaiter( );
		}
	}
	
	internal class LoadTaskData
	{
		public AsyncOperation	Operation;
		
		public UInt64			BytesDownloaded;
		public Single			StateProgress;
		public ELoadingState	LoadingState;
		public Vector3			Percentages;
		
		public Single			OverallProgress 
		{
			get
			{ 
				return LoadingState switch
				{
					ELoadingState.Download	=> Percentages[0] * StateProgress,
					ELoadingState.Unpack	=> Percentages[0] + ( Percentages[1] - Percentages[0] ) * StateProgress,
					ELoadingState.Load		=> Percentages[1] + ( Percentages[2] - Percentages[1] ) * StateProgress,
					ELoadingState.Done		=> 1,
					_ => 0
				};
			}
		}

		internal async UniTask	Release			( )		
		{
			LoadingState	= ELoadingState.Done;
			StateProgress	= 1;
			
			await UniTask.DelayFrame( 10 );

			Operation		= null;
			BytesDownloaded	= default;
			StateProgress	= default;
			LoadingState	= default;
			
			GenericPool<LoadTaskData>.Release( this );
		}
	}
}