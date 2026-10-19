using UnityEngine;
using ZTools;

public class CameraControl : MonoSingleton<CameraControl>
{
    public Transform CameraRig;
    public Camera MainCamera;

    [SerializeField]
    private float minRigZ = -140;
    [SerializeField]
    private float maxRigZ = 140;
    private float curRigZ = 0;
    private float tarRigZ = 0;

    [SerializeField]
    private float rigXSpeed = 1f;
    [SerializeField]
    private float minRigX = -40;
    private float curMinRigX = 0;
    [SerializeField]
    private float maxRigX = 40;
    private float curMaxRigX = 0;
    private float curRigX = 0;
    private float tarRigX = 0;
    

    [Header("横向拖动")]
    [SerializeField]
    private float rigZSpeed = 0.5f;
    [SerializeField]
    private bool invertHorizontalDrag = false;

    private bool isDragging;
    private Vector2 lastPointerPosition;

    public float ZoomSpeed = 1;
    private float curDis;
    private float tarDis;
    [SerializeField]
    private float maxForwardDis = 50;
    private Vector3 oriCameraPos;

    private void Awake()
    {
        curDis = 0;
        tarDis = 0;
        oriCameraPos = MainCamera.transform.localPosition;

        curRigX = 0;
        curRigZ = 0;
        tarRigX = 0;
        tarRigZ = 0;
    }

    private void Update()
    {
        HandleDrag();
        HandleZoom();   
    }

    /// <summary>
    /// 屏幕左右拖动只修改 CameraRig 的世界 Z，不修改 X/Y。
    /// 使用 minRigZ/maxRigZ 限制相机范围。
    /// </summary>
    private void HandleDrag()
    {
        // 手机触摸优先，避免同一帧同时处理鼠标模拟输入。
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                isDragging = true;
                lastPointerPosition = touch.position;
            }
            else if (isDragging && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                ApplyHorizontalDrag(touch.position - lastPointerPosition);
                lastPointerPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastPointerPosition = Input.mousePosition;
        }
        else if (isDragging && Input.GetMouseButton(0))
        {
            ApplyHorizontalDrag((Vector2)Input.mousePosition - lastPointerPosition);
            ApplyVDrag((Vector2)Input.mousePosition - lastPointerPosition);
            lastPointerPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private void ApplyHorizontalDrag(Vector2 screenDelta)
    {
        if (CameraRig == null || Mathf.Abs(screenDelta.x) < 0.001f)
            return;

        // 拖动方向默认与相机画面一致；需要相反手感时勾选 invertHorizontalDrag。
        float direction = invertHorizontalDrag ? 1f : -1f;
        tarRigZ = CameraRig.transform.position.z + screenDelta.x * rigZSpeed * direction;
        tarRigZ = Mathf.Clamp(tarRigZ, Mathf.Min(minRigZ, maxRigZ), Mathf.Max(minRigZ, maxRigZ));

        CameraRig.transform.position = new Vector3(CameraRig.position.x, 0, tarRigZ);
    }

    private void ApplyVDrag(Vector2 screenDelta)
    {
        if (CameraRig == null || Mathf.Abs(screenDelta.y) < 0.001f)
            return;

        tarRigX = CameraRig.transform.position.x + screenDelta.y * rigXSpeed;
        curMinRigX = minRigX * curDis / (maxForwardDis - 0);
        curMaxRigX = maxRigX * curDis / (maxForwardDis - 0);
        tarRigX = Mathf.Clamp(tarRigX, Mathf.Min(curMinRigX, curMaxRigX), Mathf.Max(curMinRigX, curMaxRigX));

        CameraRig.transform.position = new Vector3(tarRigX, 0, CameraRig.position.z);
    }


     private void HandleZoom()
    {
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) < 0.001f)
            return;
        float delta = wheel * 10;
        tarDis += delta;
        if (tarDis > maxForwardDis)
            tarDis = maxForwardDis;
        if (tarDis < 0)
            tarDis = 0;

        
    }

    private void LateUpdate()
    {
        lateUpdateZoom();
    }

    private void lateUpdateZoom()
    {
        curDis = Mathf.Lerp(curDis, tarDis, Time.deltaTime * ZoomSpeed);
        MainCamera.transform.localPosition = oriCameraPos + MainCamera.transform.forward.normalized * curDis;
        if(Mathf.Abs(tarDis - curDis) > 0.001f)
        {
            curMinRigX = minRigX * curDis / (maxForwardDis - 0);
            curMaxRigX = maxRigX * curDis / (maxForwardDis - 0);
            tarRigX = Mathf.Clamp(tarRigX, Mathf.Min(curMinRigX, curMaxRigX), Mathf.Max(curMinRigX, curMaxRigX));
            CameraRig.transform.position = Vector3.Lerp(CameraRig.transform.position, new Vector3(tarRigX, 0, CameraRig.transform.position.z), Time.deltaTime * ZoomSpeed); 
        }
    }


    private void lateUpdateRigPos()
    {
        CameraRig.transform.position = Vector3.Lerp(CameraRig.transform.position, new Vector3(tarRigX, 0, tarRigZ), Time.deltaTime);
    }


    //[Header("References")]
    //public Transform CameraRig;
    //public Camera MainCamera;

    //[Header("Battle Bounds (world X/Z)")]
    //// BattleDefine.AreaTotalWith 为 80，城墙宽度为 160，因此默认横向范围为 -80~80。
    //public Vector2 BattleXBounds = new Vector2(-80f, 80f);
    //public Vector2 BattleZBounds = new Vector2(-30f, 30f);
    //public float GroundY;
    //public float BoundaryPadding = 0.1f;

    //[Header("Drag")]
    //public float HorizontalDragSpeed = 1f;
    //public float VerticalDragSpeed = 1f;
    //public bool AllowVerticalDrag = true;
    //public int DragMouseButton = 0;

    //[Header("Zoom")]
    //public float ZoomSpeed = 8f;
    //public float NearDistance;
    //public float FarDistance;
    //public float NearOrthoSize = 8f;
    //public float FarOrthoSize = 24f;

    //private float _distance;
    //private Vector3 _zoomDirection;
    //private bool _dragging;
    //private Vector3 _lastDragGroundPoint;

    //private void Start()
    //{
    //    if (MainCamera == null)
    //        MainCamera = Camera.main;
    //    if (CameraRig == null || MainCamera == null)
    //    {
    //        Debug.LogError("CameraControl requires CameraRig and MainCamera.");
    //        enabled = false;
    //        return;
    //    }

    //    _distance = MainCamera.transform.localPosition.magnitude;
    //    _zoomDirection = _distance > 0.001f ? MainCamera.transform.localPosition / _distance : Vector3.down;
    //    if (FarDistance <= 0f)
    //        FarDistance = _distance;
    //    if (NearDistance <= 0f)
    //        NearDistance = FarDistance * 0.55f;
    //    NearDistance = Mathf.Max(0.01f, NearDistance);
    //    FarDistance = Mathf.Max(NearDistance, FarDistance);
    //    _distance = Mathf.Clamp(_distance, NearDistance, FarDistance);
    //    ApplyZoom();
    //}

    //private void Update()
    //{
    //    if (MainCamera == null || CameraRig == null)
    //        return;
    //    HandleZoom();
    //    HandleMouseDrag();
    //    HandleTouchDrag();
    //    ClampRigToBattle();
    //}

    //private void HandleZoom()
    //{
    //    float wheel = Input.mouseScrollDelta.y;
    //    if (Mathf.Abs(wheel) < 0.001f)
    //        return;
    //    float delta = -wheel * ZoomSpeed;
    //    if (MainCamera.orthographic)
    //        MainCamera.orthographicSize = Mathf.Clamp(MainCamera.orthographicSize + delta, NearOrthoSize, FarOrthoSize);
    //    else
    //    {
    //        _distance = Mathf.Clamp(_distance + delta, NearDistance, FarDistance);
    //        ApplyZoom();
    //    }
    //}

    //private void ApplyZoom()
    //{
    //    if (!MainCamera.orthographic)
    //        MainCamera.transform.localPosition = _zoomDirection * _distance;
    //}

    //private void HandleMouseDrag()
    //{
    //    if (Input.GetMouseButtonDown(DragMouseButton))
    //    {
    //        Vector3 point;
    //        _dragging = TryGetGroundPoint(Input.mousePosition, out point);
    //        if (_dragging)
    //            _lastDragGroundPoint = point;
    //    }
    //    if (Input.GetMouseButton(DragMouseButton) && _dragging)
    //    {
    //        Vector3 point;
    //        if (TryGetGroundPoint(Input.mousePosition, out point))
    //        {
    //            MoveRig(_lastDragGroundPoint - point);
    //            _lastDragGroundPoint = point;
    //        }
    //    }
    //    if (Input.GetMouseButtonUp(DragMouseButton))
    //        _dragging = false;
    //}

    //private void HandleTouchDrag()
    //{
    //    if (Input.touchCount != 1)
    //        return;
    //    Touch touch = Input.GetTouch(0);
    //    Vector3 point;
    //    if (touch.phase == TouchPhase.Began)
    //    {
    //        _dragging = TryGetGroundPoint(touch.position, out point);
    //        if (_dragging)
    //            _lastDragGroundPoint = point;
    //    }
    //    else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && _dragging)
    //    {
    //        if (TryGetGroundPoint(touch.position, out point))
    //        {
    //            MoveRig(_lastDragGroundPoint - point);
    //            _lastDragGroundPoint = point;
    //        }
    //    }
    //    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
    //        _dragging = false;
    //}

    //private void MoveRig(Vector3 delta)
    //{
    //    delta.y = 0f;
    //    delta.x *= HorizontalDragSpeed;
    //    delta.z *= VerticalDragSpeed;
    //    if (!AllowVerticalDrag)
    //        delta.z = 0f;
    //    CameraRig.position += delta;
    //}

    //private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point)
    //{
    //    Ray ray = MainCamera.ScreenPointToRay(screenPosition);
    //    if (Mathf.Abs(ray.direction.y) < 0.0001f)
    //    {
    //        point = Vector3.zero;
    //        return false;
    //    }
    //    float distance = (GroundY - ray.origin.y) / ray.direction.y;
    //    if (distance < 0f)
    //    {
    //        point = Vector3.zero;
    //        return false;
    //    }
    //    point = ray.origin + ray.direction * distance;
    //    return true;
    //}

    //private void ClampRigToBattle()
    //{
    //    Vector2 xFootprint;
    //    Vector2 zFootprint;
    //    CalculateGroundFootprint(out xFootprint, out zFootprint);
    //    float battleMinX = Mathf.Min(BattleXBounds.x, BattleXBounds.y);
    //    float battleMaxX = Mathf.Max(BattleXBounds.x, BattleXBounds.y);
    //    float battleMinZ = Mathf.Min(BattleZBounds.x, BattleZBounds.y);
    //    float battleMaxZ = Mathf.Max(BattleZBounds.x, BattleZBounds.y);
    //    // 横向使用战场中心范围。当前镜头在最远距离时，视锥可能比战场宽，
    //    // 如果也用视锥反推 X 范围会把 minX/maxX 压成同一个点，导致左右拖动完全没反应。
    //    // 纵向仍使用视锥计算，因此最远距离时上下范围会自动收缩为 0。
    //    float minX = battleMinX + BoundaryPadding;
    //    float maxX = battleMaxX - BoundaryPadding;
    //    float minZ = battleMinZ - zFootprint.x + BoundaryPadding;
    //    float maxZ = battleMaxZ - zFootprint.y - BoundaryPadding;
    //    float x = minX <= maxX ? Mathf.Clamp(CameraRig.position.x, minX, maxX) : (minX + maxX) * 0.5f;
    //    float z = minZ <= maxZ ? Mathf.Clamp(CameraRig.position.z, minZ, maxZ) : (minZ + maxZ) * 0.5f;
    //    Vector3 position = CameraRig.position;
    //    position.x = x;
    //    position.z = z;
    //    CameraRig.position = position;
    //}

    //private void CalculateGroundFootprint(out Vector2 xRange, out Vector2 zRange)
    //{
    //    float minX = float.PositiveInfinity;
    //    float maxX = float.NegativeInfinity;
    //    float minZ = float.PositiveInfinity;
    //    float maxZ = float.NegativeInfinity;
    //    Vector3[] corners = { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0f) };
    //    for (int i = 0; i < corners.Length; i++)
    //    {
    //        Ray ray = MainCamera.ViewportPointToRay(corners[i]);
    //        if (Mathf.Abs(ray.direction.y) < 0.0001f)
    //            continue;
    //        float distance = (GroundY - ray.origin.y) / ray.direction.y;
    //        if (distance < 0f)
    //            continue;
    //        Vector3 local = ray.origin + ray.direction * distance - CameraRig.position;
    //        minX = Mathf.Min(minX, local.x);
    //        maxX = Mathf.Max(maxX, local.x);
    //        minZ = Mathf.Min(minZ, local.z);
    //        maxZ = Mathf.Max(maxZ, local.z);
    //    }
    //    if (float.IsInfinity(minX))
    //        minX = maxX = minZ = maxZ = 0f;
    //    xRange = new Vector2(minX, maxX);
    //    zRange = new Vector2(minZ, maxZ);
    //}
}
