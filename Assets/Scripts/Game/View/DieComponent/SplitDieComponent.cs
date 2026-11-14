using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 冰冻角色死亡时的碎裂表现：隐藏原角色，将当前帧网格切成多个碎片向外飞散，全部落地一秒后回收角色。
/// 该组件由 UnitView 在检测到 Freeze Buff 时自动使用，也支持通过 dieType=Split 主动指定。
/// </summary>
public class SplitDieComponent : DieBaseComponent
{
    private const int MinPieceCount = 8;
    private const int MaxPieceCount = 10;
    private const float GroundStayDuration = 1f;
    private const float ShardSpeedMin = 1.5f;
    private const float ShardSpeedMax = 4.5f;
    private const float ShardGravity = 7f;
    private const float GroundY = 0f;
    private const string SplitShaderName = "WorldIsMine/IceShatter";

    private readonly List<ShardMotion> shardMotions = new List<ShardMotion>();
    private readonly List<RendererState> hiddenRenderers = new List<RendererState>();
    private GameObject piecesRoot;
    private float groundedStayElapsed;
    private float groundY;
    private bool playing;

    /// <summary>
    /// 创建冰冻碎裂死亡表现组件。
    /// </summary>
    protected override void EnterDieState()
    {
        groundedStayElapsed = 0f;
        playing = true;
        Shader shardShader = Shader.Find(SplitShaderName);
        if (shardShader == null)
        {
            shardShader = Shader.Find("Unlit/Color");
        }
        piecesRoot = MeshSplitUtility.AcquirePiecesRoot(mUnitView.name + "_IceShards", transform.position);
        groundY = GroundY;

        int pieceCount = Random.Range(MinPieceCount, MaxPieceCount + 1);
        List<MeshSplitPiece> pieces = MeshSplitUtility.CreatePieces(
            mUnitView.gameObject,
            piecesRoot.transform,
            pieceCount,
            shardShader,
            Random.Range(1, int.MaxValue));
        HideOriginalRenderers();

        Vector3 origin = transform.position;
        for (int i = 0; i < pieces.Count; i++)
        {
            MeshSplitPiece piece = pieces[i];
            Vector3 direction = piece.GameObject.transform.position - origin;
            direction += Random.insideUnitSphere * 0.45f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Random.onUnitSphere;
            }

            direction.Normalize();
            float speed = Random.Range(ShardSpeedMin, ShardSpeedMax);
            Vector3 angularVelocity = new Vector3(
                Random.Range(-180f, 180f),
                Random.Range(-180f, 180f),
                Random.Range(-180f, 180f));
            SetShardMaterial(piece.Renderer);
            shardMotions.Add(new ShardMotion(piece, direction * speed, angularVelocity));
        }
    }

    /// <summary>
    /// 驱动碎片飞散并落到 Y=0，全部落地一秒后直接隐藏碎片并回收角色。
    /// </summary>
    protected override void UpdateDie(float dt)
    {
        if (!playing)
        {
            return;
        }

        bool allGrounded = true;
        for (int i = 0; i < shardMotions.Count; i++)
        {
            ShardMotion motion = shardMotions[i];
            if (motion.Piece.GameObject == null)
            {
                continue;
            }

            if (!motion.Grounded)
            {
                motion.Velocity += Vector3.down * ShardGravity * dt;
                motion.Piece.GameObject.transform.position += motion.Velocity * dt;
                motion.Piece.GameObject.transform.Rotate(motion.AngularVelocity * dt, Space.Self);
                Bounds bounds = motion.Piece.Renderer.bounds;
                if (bounds.min.y <= groundY)
                {
                    motion.Piece.GameObject.transform.position += Vector3.up * (groundY - bounds.min.y);
                    motion.Velocity = Vector3.zero;
                    motion.Grounded = true;
                }
            }

            if (!motion.Grounded)
            {
                allGrounded = false;
            }
        }

        if (!allGrounded)
        {
            groundedStayElapsed = 0f;
            return;
        }

        groundedStayElapsed += dt;
        if (groundedStayElapsed < GroundStayDuration)
        {
            return;
        }

        playing = false;
        CleanupPieces();
        CycleUnitView();
    }

    /// <summary>
    /// 隐藏原角色的所有渲染器，让画面只保留网格碎片。
    /// </summary>
    private void HideOriginalRenderers()
    {
        Renderer[] renderers = mUnitView.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].transform.IsChildOf(piecesRoot.transform))
            {
                continue;
            }

            hiddenRenderers.Add(new RendererState(renderers[i], renderers[i].enabled));
            renderers[i].enabled = false;
        }
    }

    /// <summary>
    /// 设置碎片的冰晶颜色、切割面颜色和初始不透明度。
    /// </summary>
    private void SetShardMaterial(Renderer renderer)
    {
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetColor("_IceTint", new Color(0.42f, 0.82f, 1f, 1f));
        propertyBlock.SetColor("_CutColor", new Color(0.8f, 0.96f, 1f, 1f));
        // 旧参数：碎裂效果使用独立冰晶纹理强度，和角色的 _ICEState 不一致。
        // propertyBlock.SetFloat("_IceStrength", 0.72f);
        // propertyBlock.SetFloat("_IceStrength", 1f);
        // 新参数：碎裂效果与角色冰冻表现统一使用 0.5 的冰冻叠加强度。
        propertyBlock.SetFloat("_ICEState", 0.7f);
        propertyBlock.SetFloat("_CutStrength", 0.95f);
        renderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// 将碎片对象放回局部对象池，并恢复原角色渲染器供角色对象池复用。
    /// </summary>
    private void CleanupPieces()
    {
        for (int i = 0; i < hiddenRenderers.Count; i++)
        {
            if (hiddenRenderers[i].Renderer != null)
            {
                hiddenRenderers[i].Renderer.enabled = hiddenRenderers[i].WasEnabled;
            }
        }
        hiddenRenderers.Clear();

        for (int i = 0; i < shardMotions.Count; i++)
        {
            MeshSplitPiece piece = shardMotions[i].Piece;
            MeshSplitUtility.RecyclePiece(piece);
        }
        shardMotions.Clear();

        if (piecesRoot != null)
        {
            MeshSplitUtility.RecyclePiecesRoot(piecesRoot);
            piecesRoot = null;
        }
    }

    /// <summary>
    /// 组件被销毁时兜底清理碎片，避免对象池提前回收留下运行时对象。
    /// </summary>
    private void OnDestroy()
    {
        CleanupPieces();
    }

    /// <summary>
    /// 角色对象提前禁用或进入对象池时立即恢复隐藏的 Mesh，并清理尚未结束的碎片表现。
    /// </summary>
    private void OnDisable()
    {
        playing = false;
        CleanupPieces();
    }

    /// <summary>
    /// 保存单个碎片的运动状态。
    /// </summary>
    private sealed class ShardMotion
    {
        public readonly MeshSplitPiece Piece;
        public Vector3 Velocity;
        public readonly Vector3 AngularVelocity;
        public bool Grounded;

        /// <summary>
        /// 初始化碎片运动状态。
        /// </summary>
        public ShardMotion(MeshSplitPiece piece, Vector3 velocity, Vector3 angularVelocity)
        {
            Piece = piece;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
        }
    }

    /// <summary>
    /// 保存原角色 Renderer 在碎裂前的显示状态，回收前按原状态恢复，避免对象池复用时误显示隐藏模型。
    /// </summary>
    private sealed class RendererState
    {
        public readonly Renderer Renderer;
        public readonly bool WasEnabled;

        /// <summary>
        /// 保存 Renderer 引用及其原始启用状态。
        /// </summary>
        public RendererState(Renderer renderer, bool wasEnabled)
        {
            Renderer = renderer;
            WasEnabled = wasEnabled;
        }
    }
}
