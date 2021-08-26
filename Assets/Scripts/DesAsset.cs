using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;
using UnityEngine.UI;

public class DesAsset : MonoBehaviour
{
    public AssetReference prefabRef;
    public AssetReferenceAtlasedSprite AtlasedSpriteRef;
    public Image image;
    public AssetReferenceTexture textureRef;
    public GameObject Cube;
    public GameObject SenceCube;
    private void Start()
    {
        prefabRef.InstantiateAsync().Completed += (args) =>
        {
            Cube = args.Result;
            Cube.GetComponent<MeshRenderer>().material.mainTexture = textureRef.Asset as Texture;
        };
        AtlasedSpriteRef.LoadAssetAsync<SpriteAtlas>().Completed+=(texs) =>
        {
            Debug.Log(texs.Result.name);
            Sprite[] spriteArray = new Sprite[texs.Result.spriteCount];
            //spriteArray得到数组
            texs.Result.GetSprites(spriteArray);
            foreach (var VARIABLE in spriteArray)
            {
                Debug.Log(VARIABLE.name);
            }
            image.sprite =spriteArray[0] as Sprite;
        };
        ///Lamda 表达式回调方式
        textureRef.LoadAssetAsync<Texture>().Completed += (texs) =>
        {
            Debug.Log(texs.Result.name);
            SenceCube.GetComponent<MeshRenderer>().material.mainTexture = texs.Result as Texture;
        };
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            prefabRef.ReleaseInstance(Cube);
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            textureRef.ReleaseAsset();
        }
    }
}