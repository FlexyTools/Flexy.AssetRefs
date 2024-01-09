#if FLEXY_UI

namespace Flexy.Boot.View
{
	public class Window_LoadUpdateBundles : UIWindow
	{
		[Bindable] StateLoadBundles StateBundleLoad { get => _stateBundleLoad; set { _stateBundleLoad = value; /*RebindProperty( nameof(StateBundleLoad) );*/ } }

		#if FLEXY_BUNDLES
		[SerializeField] 	Flexy.AssetBundles.BundleRef				_mandatoryBundles;
		public BundlesDownloadingData BundlesDownloadingData = new BundlesDownloadingData(  );
		#endif

		#if FLEXY_BUNDLES
		[Bindable]  Single				FullDownloadSize	=> (Single) Math.Round(BundlesDownloadingData.FullDownloadSize / 1_000_000f, 2);
		[Bindable]  Single				DownloadedSize		=> (Single) Math.Round(BundlesDownloadingData.DownloadedSize / 1_000_000f, 2);
		[Bindable]  Single				DownloadSpeed		=> (Single) Math.Round(BundlesDownloadingData.DownloadSpeed / 1_000_000f, 2);
		#endif


		protected override void OnOpen()
		{
			RunLoadBundles().Forget();
		}

		private async UniTask RunLoadBundles( )
		{
			#if FLEXY_BUNDLES
			Debug.LogBoot			( $"[LoadBundlesStep] - LoadBundles: Start" );
			
			while ( !Flexy.AssetBundles.BundleManager.IsBundlesMounted( _mandatoryBundles.BundleNames ) )
			{
				SetStateBundleLoad(StateLoadBundles.Prepare);
			
				if ( Application.internetReachability == NetworkReachability.NotReachable )
				{
					SetStateBundleLoad(StateLoadBundles.NoConnection);
					await UniTask.WaitWhile( () => Application.internetReachability == NetworkReachability.NotReachable );
					continue;
				}

				if ( Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork && !Flexy.AssetBundles.BundleManager.IsCellularDataConfirm )
				{
					SetStateBundleLoad(StateLoadBundles.ConfirmCellularData);
					await UniTask.WaitWhile( () => Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork && !Flexy.AssetBundles.BundleManager.IsCellularDataConfirm );
					continue;
				}

				Debug.LogBoot			( $"[BootClientView] - LoadBundles: Download mandatory Bundles: {_mandatoryBundles.BundleNames.JoinToString()}" );

				BundlesDownloadingData = new BundlesDownloadingData(  );
				
				await BundlesDownloadingData.UpdateNeedDownloadSizeByBundleNames( _mandatoryBundles.BundleNames );
				
				BundlesDownloadingData.DownloadTask = UniTask.Create( async ( ) => 
				{
					await Flexy.AssetBundles.BundleManager.DownloadBundles( _mandatoryBundles.BundleNames, Progress.CreateOnlyValueChanged<Single>( value => BundlesDownloadingData.DownloadProgress = value) );
				}).Preserve(  );
				
				SetStateBundleLoad(StateLoadBundles.Load);
				
				await BundlesDownloadingData.DownloadTask;

			}

			Debug.LogBoot			( $"[LoadBundlesStep] - LoadBundles: DONE" );
			SetLoadProgress( 1f );
			await UniTask.Delay( 500 );
			#endif
			
			Close( );
		}

		public void SetStateBundleLoad	( StateLoadBundles state )	
		{
			StateBundleLoad = state;
		}

		[Callable] void UseCellularData		( )							
		{
			#if FLEXY_BUNDLES
			Flexy.AssetBundles.BundleManager.IsCellularDataConfirm = true;
			#endif
		}

		private void Update				( )							
		{
			#if FLEXY_BUNDLES
			if (BundlesDownloadingData.IsDownloading)
				_loadProgress = BundlesDownloadingData.DownloadProgress;
			#endif
		}

		private StateLoadBundles _stateBundleLoad = StateLoadBundles.Prepare;



		public enum StateLoadBundles : Byte
		{
			Prepare             = 0,
			NoConnection        = 1,
			Load                = 2,
			ConfirmCellularData = 3
		}
	}
}

#endif