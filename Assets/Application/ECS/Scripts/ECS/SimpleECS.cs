﻿using System.Collections;
using System.Collections.Generic;
using Unity.Entities;


using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace AppECSCode
{
    public struct MyTransform : IComponentData
    {
        public UnityEngine.Vector3 position;
    }

    public struct CharaData : IComponentData
    {
        public Vector3 velocity;
        public float time;
        public int rectIndex;
    }

    [DisableAutoCreation]
    public class ECSCharaSystem : ComponentSystem
    {


        // ランニング用アニメーションの情報
        public AppAnimationInfo animationInfo;
        // 描画用のメッシュ
        public Mesh drawMesh;
        // 描画用のマテリアル
        public Material drawCharaMaterial;
        // 影描画用のマテリアル
        public Material drawShadowMaterial;


        // ZPrepass用のコマンド
        private CommandBuffer zPrepassCommandBuffer;
        // 実際の描画コマンド
        private CommandBuffer actualCommandBuffer;

        // アニメーション用
        private new NativeArray<Vector4> animationVectorInfo;

        //GPUインスタンシングで一括でかく数
        private const int InstanceDrawNum = 500;
        // GPU インスタンスで一気に書くときのバッファー
        private Matrix4x4[] instancedBufferForMatrics = new Matrix4x4[InstanceDrawNum];
        // GPU インスタンスで一気に書くときのバッファー
        private Vector4[] instancedBufferForRects = new Vector4[InstanceDrawNum];

        // material property
        private MaterialPropertyBlock materialBlock;

        public int CharaNum
        {
            get { return characterNum; }
        }
        private int characterNum = 0;

        public ECSCharaSystem() { 
            zPrepassCommandBuffer = new CommandBuffer();
            actualCommandBuffer = new CommandBuffer();
            Camera.main.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, zPrepassCommandBuffer);
            Camera.main.AddCommandBuffer(CameraEvent.AfterForwardOpaque, actualCommandBuffer);

        }

        /// <summary>
        ///  パラメーター設定後 手動で呼び出します
        /// </summary>
        public void Setup()
        {
            // Animation情報
            animationVectorInfo = new NativeArray<Vector4>(animationInfo.Length, Allocator.Persistent);
            for (int i = 0; i < animationInfo.Length; ++i)
            {
                var rect = animationInfo.GetUvRect(i);
                animationVectorInfo[i] = new Vector4(rect.x, rect.y, rect.width, rect.height);
            }
            materialBlock = new MaterialPropertyBlock();
        }


        protected override void OnUpdate()
        {
            var charaGroup = this.EntityManager.CreateComponentGroup(typeof(MyTransform), typeof(CharaData));
            var entities = charaGroup.GetEntityArray();
            var charas = charaGroup.GetComponentDataArray<CharaData>();
            var transforms = charaGroup.GetComponentDataArray<MyTransform>();

            var deleteEntities = new NativeList<Entity>(0, Allocator.Temp);
            float deltaTime = Time.deltaTime;

            int animationLength = animationInfo.animationLength;
            Vector3 cameraPosition = Camera.main.transform.position;

            characterNum = charas.Length;
            // 更新処理
            for (int i = 0; i < charas.Length; ++i)
            {
                // データ取得
                MyTransform transform = transforms[i];
                CharaData chara = charas[i];

                //移動処理
                transform.position += chara.velocity * deltaTime;
                //時間の追加
                chara.time += deltaTime;


                Vector3 forwardFromCamera = GetVectorFromCamera(cameraPosition, transform.position, chara.velocity);
                int direction = AppAnimationInfo.GetDirection(forwardFromCamera);//<-カメラと、キャラクターの向きを考慮してどの向きを向くかを決定します
                chara.rectIndex = ((int)(chara.time * 25.0f)) % animationLength + (direction * animationLength);

                // 削除処理
                if (chara.time > 8.0f)
                {
                    deleteEntities.Add(entities[i]);
                }
                // 書き戻し
                transforms[i] = transform;
                charas[i] = chara;
            }

            // 描画処理
            zPrepassCommandBuffer.Clear();
            actualCommandBuffer.Clear();

            int drawNum = 0;
            for (int i = 0; i < charas.Length; ++i)
            {
                instancedBufferForMatrics[i] = CreateMatrix(transforms[i].position, cameraPosition);
                instancedBufferForRects[i] = animationVectorInfo[charas[i].rectIndex];
                ++drawNum;
            }
            if (drawNum > 0)
            {
                materialBlock.SetVectorArray(ShaderNameHash.RectValue, instancedBufferForRects);
                zPrepassCommandBuffer.DrawMeshInstanced(this.drawMesh, 0, this.drawCharaMaterial, 0, instancedBufferForMatrics, drawNum, materialBlock);
                actualCommandBuffer.DrawMeshInstanced(this.drawMesh, 0, this.drawCharaMaterial, 1, instancedBufferForMatrics, drawNum, materialBlock);
            }
            charaGroup.Dispose();

            // 削除リストにあったEntityの削除
            Debug.Log("deleteTntities:" + deleteEntities.Length);
            this.EntityManager.DestroyEntity(deleteEntities);

            deleteEntities.Dispose();

            Debug.Log("CHaras:" + charas.Length + ":" + entities.Length );
        }

        public void OnDestroy()
        {
            animationVectorInfo.Dispose();
        }


        /// <summary>
        /// マトリックスを直接計算します
        /// </summary>
        private static Matrix4x4 CreateMatrix(Vector3 position, Vector3 cameraPos)
        {
            var diff = position - cameraPos;
            diff.Normalize();

            Matrix4x4 matrix = Matrix4x4.identity;
            // 向きセット
            matrix.m00 = diff.z;
            matrix.m02 = -diff.x;

            matrix.m20 = -diff.x;
            matrix.m22 = diff.z;

            // 位置セット
            matrix.m03 = position.x;
            matrix.m13 = position.y;
            matrix.m23 = position.z;
            return matrix;
        }


        /// <summary>
        /// カメラを考慮して向きを決定します
        /// </summary>
        private static Vector3 GetVectorFromCamera(Vector3 cameraPos, Vector3 charaPos, Vector3 charaForward)
        {
            Vector3 diff = charaPos - cameraPos;
            Vector3 fromCameraForward = new Vector3(
                diff.z * charaForward.x - diff.x * charaForward.z,
                0.0f,
                diff.x * charaForward.x + diff.z * charaForward.z);

            return fromCameraForward;
        }


    }
}