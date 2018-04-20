using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// native container関連
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
//job関連
using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

// ECS
using Unity.Entities;

//using Unity.Entities;
using AppECSCode;

// ECSのエントリー用コード
public class ECSEntry : MonoBehaviour
{
    // ランダム出現位置に関するぱらえーた
    private const float InitPosXParam = 22.5f;
    private const float InitPosZParam = 15.0f;

    // ランニング用アニメーションの情報
    public AppAnimationInfo misakiAnime;
    // ランニング用アニメーションの情報
    public AppAnimationInfo YukoAnime;
    // 描画用のメッシュ
    public Mesh drawMesh;
    // 描画用のマテリアル
    public Material drawCharaMaterial;
    // 影描画用のマテリアル
    public Material drawShadowMaterial;

    // キャラクター数
    public int clickInstance = 100;

    #region ECS_VARS
    // EntityManager
    private EntityManager manager;
    // 実行用のCharaSystem
    private ECSCharaSystem charaSystem;
    // 生成用のArcheType
    private EntityArchetype charaArch;
    #endregion ECS_VARS


    /// <summary>
    /// Start関数
    /// </summary>
    void Start()
    {
        // animation の情報初期化
        misakiAnime.Initialize();
        YukoAnime.Initialize();
 //           var rect = animationInfo.GetUvRect(i);



        // EntityManager
        manager = World.Active.GetOrCreateManager<EntityManager>();
        // Systemを作成してセット
        charaSystem = World.Active.GetOrCreateManager<ECSCharaSystem>();
        charaSystem.animationInfo = misakiAnime;
        charaSystem.drawMesh = drawMesh;
        charaSystem.drawCharaMaterial = new Material(drawCharaMaterial);
        charaSystem.drawCharaMaterial.mainTexture = misakiAnime.texture;
        charaSystem.drawShadowMaterial = drawShadowMaterial;
        charaSystem.Setup();
        // chara生成用のArcheTypeを作成
        charaArch = manager.CreateArchetype(typeof(MyTransform), typeof(CharaData));
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0) )
        {
            // Entityセット用のデータ作成
            MyTransform trans = new MyTransform();
            CharaData chara = new CharaData();

            for (int i = 0; i < clickInstance; ++i)
            {
                // Entityの作成
                var entity = manager.CreateEntity(charaArch);
                //座標セット
                float x = -InitPosXParam + i * 2 * InitPosXParam / clickInstance;
                trans.position = new Vector3( x,0.5f,-InitPosZParam);
                // キャラクターのデータセット
                chara.time = Random.Range( 0.0f , 3.0f); // 少し揺れ幅を持たせます
                chara.velocity = new Vector3(0.0f, 0.0f, 3.0f);
                // 生成したEntitiyにデータセット
                manager.SetComponentData(entity, trans);
                manager.SetComponentData(entity, chara);
            }
        }
        // デバッグ情報更新
        DebugUI.SetCharaNum(charaSystem.CharaNum);
        charaSystem.Update();
    }
    void OnDestroy()
    {
        charaSystem.OnDestroy();
    }
}
