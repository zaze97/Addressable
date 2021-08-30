using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using UnityEngine.UI;

public class DesAsset : MonoBehaviour
{
    public AddressableManager AddressableManager;
   // public AssetReferenceAtlasedSprite AtlasedSpriteRef;
    public Image image;
    public Texture textureRef;
    public GameObject Cube;
    public GameObject SenceCube;
    private void Start()
    {
        AddressableManager.LoadAsset<Texture>("Local/Model/home_bg_2560 2", (texs) =>
        {
            Debug.Log(texs.name);
            textureRef = texs;
            SenceCube.GetComponent<MeshRenderer>().material.mainTexture = texs;
        }, () =>
        {
            Debug.LogError("初始化失败");
        });
        AddressableManager.InstantiateAsset("Local/Model/Cube1", (args) =>
        {
            Cube = args;
            Cube.GetComponent<MeshRenderer>().material.mainTexture = textureRef;
        });
        
        AddressableManager.LoadTagAsset<GameObject>("Model", (ag) => {Debug.Log(ag+ "加载完成"); },(args) =>
            {
                Debug.Log( "全部加载完成"); 
            });
        
        
        
        AddressableManager.LoadAsset<SpriteAtlas>("Remoted/SpriteAtlas/Loading", (texs) =>
        {
            Debug.Log(texs.name);
            // Sprite[] spriteArray = new Sprite[texs.Result.spriteCount];
            // //spriteArray得到数组
            // texs.Result.GetSprites(spriteArray);
            // foreach (var VARIABLE in spriteArray)
            // {
            //     Debug.Log(VARIABLE.name);
            // }
            // image.sprite =spriteArray[0] as Sprite;
            
            
            image.sprite =texs.GetSprite("home_bg_3840 1");
        });
        ///Lamda 表达式回调方式
        // textureRef.LoadAssetAsync<Texture>().Completed += (texs) =>
        // {
        //     Debug.Log(texs.Result.name);
        //     SenceCube.GetComponent<MeshRenderer>().material.mainTexture = texs.Result as Texture;
        // };

    }


}