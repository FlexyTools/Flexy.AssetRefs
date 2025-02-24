**Flexy.AssetRefs Docs**
==================== 

Main Concepts
--------

- AssetRef
- SceneRef
- LoadExtensions
- AssetLoader
- Pipeline

You define **AssetRefs** and **SceneRefs** anywhere you want and they show up in inspector as regular unity object references  

    [SerializeField]    AssetRef<Sprite>        _icon;
    [SerializeField]    AssetRef<GameObject>    _prefab;
    [SerializeField]    SceneRef                _map_01;

Then you can load them using LoadExtensions  
They are **C# Extension methods** so you can write your own for your project needs

    using Flexy.AssetRefs.LoadExtensions;

    ...

    private void PopulateIcon ( )
    {
        _image.sprite = _icon.LoadAssetSync( );
    }

    private async UniTask PopulateIconAsync ( )
    {
        _image.sprite = await _icon.LoadAssetAsync( );
    }

    private async UniTask PopulateIconAsync ( )
    {
        await _map_01.LoadSceneAsync( );
    }

**LoadExtensions** responsible for loading asset/scene by Ref and **no one cares** how they do this!  
Default implementation directly use AssetLoader Api to load assets  

**AssetLoader** is class responsible for loading assets/scenes in Editor / PlayMode / Build  
In Editor AssetLoader loads assets using AssetDatabase in PlayMode with RuntimeMode enabled or in Build  
it uses runtime implementation that you **can override!**  
To enable RuntimeMode go **Tools/Flexy/AssetRefs/AssetLoader/Enable Runtime Behavior** 

**By Default** you have installed AssetLoader_Resources that have runtime implementation that loads AssetsFrom   
using Unity Resources Api. But you **do not** need to place your assets into Resources folder! Thanks to **Pipeline**  

**Pipeline** is SciptableObject with list of **Tasks** that collects Refs and make sure they can be loaded in runtime  
this is where **magic** happens :) 

You need to add at least one pipeline asset to project through Create Menu > Flexy > AssetRefs > Pipeline   
Simple working pipeline must consist of Tasks (in order):

>- **RunOnBuildPreprocess** - it will launch this pipeline as PreBuild step (only if it first task)
>- Few Tasks that collects refs to objects
>  - **AddRefsDirect** - add references to objects directly (how it useful later in doc)  
>  - **AddRefsFromDirectory** - with this you can collect all sprites form directory with subdirectories
>- **ResourcesPopulateRefs** - will make collected refs available for loading in runtime (without touching source assets) 

With **correct** pipeline your refs will work in editor and in build seamlessly!

What is Correct pipeline
--------

Correct pipeline is such pipeline that collects refs to all assets you want to load in runtime.

**How to make One?**

One way is:

    Add all sprite refs           using AddRefsFromDirectory
    Add all map scene refs        using AddRefsFromDirectory
    Add all Units Prefab refs     using AddRefsFromDirectory
    Add all UIWindow Prefab refs  using AddRefsFromDirectory
    Add specific assets           using AddRefsDirect

    And you are ready to go

Another way is (for case you have Game Design Data as set of ScriptableObject's):

    Place all references to assets/scenes into DgData as AssetRefs.SceneRefs
    Implement on RootGdData ScriptableObject interafce IAssetRefsSource
    Add ref to RootGdData ScriptableObject using AddRefsDirect
    Load OnDemand assets/scenes from GdData ojects refs

    And you are ready to go

For bigger project second way is preferable because your GameDesigners will create new stuff and 
will set up new refs to sprites, prefabs, scenes etc. and everything will work without any additional setup 
because pipeline will collect all refs from GdData on build.

To collect AssetRefs and SceneRefs from fields on GdData objects you have ready to use method **RefsCollector.CollectRefsDeep**
that you can use in your implementation of IAssetRefsSource

From Experience - Big Production Projects often have pipeline structure like this:

    AddRefsDirect - for few very specific objects
    AddRefsDirect - for RootGdData          object that implements IAssetRefsSource
    AddRefsDirect - for UIWindowsLibrary    object that implements IAssetRefsSource

Additionally you can write project specific PipelineTasks that collect Refs your way or do actually whatever you want  
You can use Pipelines to do various automation tasks. Collecting AssetRefs is just specific use case.

Pipeline Task ResourcesPopulateRefs will create specific assets in path Assets/Resources/Fun.Flexy/AssetRefs/  
they need to wire up loading of collected assets from Unity Resources Api and this path can be ignored in SourceControl 

One more thing - AssetLoader
--------

Async methods of asset loader return specific Task structure that have few useful properties

    var loadBossTask = _bossPrefab.LoadAssetAsync( );
    
    // You can access loading progress, check IsDone and IsSuccess
    while ( !loadBossTask.IsDone )
    {
        progressLabel.text = loadBossTask.Progress.ToString( );
        await UniTask.NextFrame( );
    }

    if( loadBossTask.IsSuccess )
        SpawnBoss( loadBossTask.GetResult( ) );

Same with Scene Loading but scene specific

    var loadBossRoomTask = _bossRoom.LoadSceneAsync( this.gameObject, new LoadSceneTask.Parameters { ActivateOnLoad = false } );

    // You can access loading progress, check IsDone, get scene struct, WaitForSceneLoadStart and AllowSceneActivation

    await loadBossRoomTask.WaitForSceneLoadStart( );

    // after scene started loading scene struct is available and we can use any scene related api before scne loading finishes
    vae scene = loadBossRoomTask.Scene;

    while ( !loadBossRoomTask.IsDone )
    {
        progressLabel.text = loadBossTask.Progress.ToString( );
        await UniTask.NextFrame( );
    }

    await UniTask.Delay( 10_000 ); 

    loadBossTask.AllowSceneActivation( );
    
    await loadBossTask; // wait for activation

    // Scene fully loaded and activated

Additionally SceneRef has static Method to load DummyScene - it will create new scene with Camera and AudioListener in it

    await SceneRef.LoadDummySceneAsync( );
    
    // or

    var dummySceneLoadTask = SceneRef.LoadDummySceneAsync( );

Finally
--------

**Warning**  
you can not **store** LoadTasks they rented from pool and will be returned to pool after 10 frames once loading done 

**Note**  
Package uses C# 10 and C# 8 Nullable reference types. All nullability warnings treated as errors.