**Flexy.AssetRefs FAQ**
==================== 

**Q: I am loading assets by direct references what the benefits of using AssetRefs?**
> - load time & memory optimisation - big assets that don't need to be loaded from the start will not consume memory and loading time e.g. BossPrefab 
> 
> **how to migrate**  
> Replace references like GameObject or Sprite with AssetRef<GameObject> AssetRefs<Sprite> accordingly  
> use _assetRef.LoadAssetSync( ) to load it when you need


**Q: I am loading assets from Resources by names what the benefits of using AssetRefs?**
> - you lost Unity ability to easily move or rename assets because their names will change and you need to keep it in sync
> - you need to make sure that there no 2 assets with the same name in different Resources folders in project
> - you need to move assets to Resources folder from you project structure
> - you need to store asset addresses as String somewhere. this is very error-prone
> - you can accidentally try to load with wrong type
> - Flexy.AssetRefs free you from all this. Move and rename assets as you wish and forget about name collisions or type mismatch
>
> **how to migrate**  
> hardcoded strings can be replaced with hardcoded guids but better to add AssetRef field
> string fields replace with AssetRefs<T> with correct type and use _assetRef.LoadAssetSync/Async instead of Resources.LoadSync/Async


**Q: I am loading assets from Addresables what the benefits of using AssetRefs?**
> - if you used each and every Addressables feature and setting and it is comfortable to you than there is no benefits :)
> - but if you fell Addressables too complicated or bloaded or hard to setup or just overcomplicated for you poroject then AssetRefs will help you
> - simpler to load assets and bugfix
> - simpler to setup assets for ondemand loading
> - simpler to reference asset without need to manage it in one more place
>
> **how to migrate**  
> replace all AssetReference fields with AssetRef fileds and update call to LoadAssetAsync/Sync  
> Flexy.AssetRefs dont allow to load by name so Update such places with proper AssetRef<T> field


**Q: Is it handle any memory optimizations like addressables?**
> Few API's for automatic unload handling in the works, But!  
> For medium to big games, there are very few places that need attention: Shop, Arsenal/Garage, Skins window where you load HD Previews while jumping between items so you need to unload them asap  
> Other places almost never need this, assets will be unloaded during loading screen
>
> So having to deal with complexity of Addressables in every corner on such project is just production overhead. Lets Keep things simple  
>
> Addresables melt concept of AssetReference and Bundles Management into one system  
> It is main thing we dislike
>
> So we created separate:
> - **Flexy.AssetRefs** - is about AssetReference with backend for AssetLoading (you can plug Addressables here if you wish) works on its own
> - **Flexy.Bundles** (to be released) - is Bundles building and management, Works as backend of Flexy.AssetRefs
>
> Currently we dont have some very smart systems to support big open  world games with seamless transitions (without loading screens)  
> But we try to figure out how my API can support it through customization  points