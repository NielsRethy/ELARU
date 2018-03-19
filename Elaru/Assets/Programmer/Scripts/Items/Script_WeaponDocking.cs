using UnityEngine;

public class Script_WeaponDocking : Script_Singleton<Script_WeaponDocking>
{
    //Dock objects -> Need trigger
    public GameObject Dock1 = null;
    public GameObject Dock2 = null;

    //Vars to see if docks are available
    private bool _dock1Free = true;
    private bool _dock2Free = true;

    //Vars to rescale docks when undocking
    private Vector3 _dock1ExtentsSave;
    private Vector3 _dock2ExtentsSave;

    [Space(10)]
    [SerializeField]
    private MeshFilter _meshFilterDock1 = null;
    [SerializeField]
    private MeshFilter _meshFilterDock2 = null;

    [SerializeField]
    private MeshRenderer _rendererDock1 = null;
    [SerializeField]
    private MeshRenderer _rendererDock2 = null;

    private Color _colorInitDock = Color.clear;
    private const string _shaderID = "_Fresnel";

    private const string TagGun = "Gun";
    private const string TagSword = "Sword";

    [Space(10)]
    [SerializeField]
    private Mesh _meshGun = null;
    [SerializeField]
    private Mesh _meshSword = null;

    private const float _scaleGun = .65f;
    private const float _scaleSword = .2f;
    private Vector3 _offsetGun = new Vector3(-0.06f, -0.05f, -0.03f);
    private Vector3 _offsetSword = new Vector3(0.14f, 0f, 0f);
    private Vector3 _rotationGun = new Vector3(135f, -180f, -90f);
    private Vector3 _rotationSword = new Vector3(0f, -90f, 45f);
    private Vector3 _rotationOffsetGun = new Vector3(270f, 0f, 180f);

    private Script_PlayerInformation _scriptPlayer = null;
    private Script_TactileFeedback _scriptFeedback = null;
    private const ushort _hapticPulseStrength = 1500;

    // Cache dock and weapon pickUp scripts, colliders and rigidbodies (less get component)
    private Script_PickUpObject _scriptPickUpObjDock1 = null;
    private Script_PickUpObject _scriptPickUpObjDock2 = null;
    private Script_PickUpObject _scriptPickUpObjGun = null;
    private Script_PickUpObject _scriptPickUpObjSword = null;
    private BoxCollider _colliderDock1 = null;
    private BoxCollider _colliderDock2 = null;
    private BoxCollider _colliderGun = null;
    private BoxCollider _colliderSword = null;
    private Rigidbody _rigidGun = null;
    private Rigidbody _rigidSword = null;

    private float _dropDistance = .2f;

    private enum WeaponType
    {
        None,
        Gun,
        Sword
    }

    private WeaponType _typeDock1 = WeaponType.None;
    private WeaponType _typeDock2 = WeaponType.None;

    private void Awake()
    {
        _scriptPlayer = Script_PlayerInformation.Instance;
        _scriptFeedback = Script_TactileFeedback.Instance;
    }

    private void Start()
    {
        //Set up actions for dock entering and leaving
        var ds = Dock1.GetComponent<Script_CollisionArea>();
        ds.TriggerEnterAction += Dock1Enter;
        ds.TriggerLeaveAction += Dock1Leave;

        ds = Dock2.GetComponent<Script_CollisionArea>();
        ds.TriggerEnterAction += Dock2Enter;
        ds.TriggerLeaveAction += Dock2Leave;

        //Set dock heights according to player
        UpdateDockingHeight();

        //Get mesh filter from dock's children
        if (_meshFilterDock1 == null)
            _meshFilterDock1 = Dock1.GetComponentInChildren<MeshFilter>();
        if (_meshFilterDock2 == null)
            _meshFilterDock2 = Dock2.GetComponentInChildren<MeshFilter>();

        // Get mesh renderer from the mesh filter
        if (_rendererDock1 == null)
            _rendererDock1 = _meshFilterDock1.GetComponent<MeshRenderer>();
        if (_rendererDock2 == null)
            _rendererDock2 = _meshFilterDock2.GetComponent<MeshRenderer>();


        // Set the initial color
        _colorInitDock = _rendererDock1.material.GetColor(_shaderID);

        // Hide the docking stations initially
        _rendererDock1.material.SetColor(_shaderID, Color.clear);
        _rendererDock2.material.SetColor(_shaderID, Color.clear);

        //Get box collider from docking stations
        _colliderDock1 = Dock1.GetComponent<BoxCollider>();
        _colliderDock2 = Dock2.GetComponent<BoxCollider>();
    }

    public void UpdateDockingHeight()
    {
        //Update heights
        var wh = _scriptPlayer.PlayerWaistHeight;

        //Standard value when height loading fails
        if (wh < 1e-5)
            wh = Script_SaveFileManager.DefaultHeight / 2f;

        //Update dock 1 height
        var pos = Dock1.transform.position;
        pos.y = wh;
        Dock1.transform.position = pos;

        pos = Dock2.transform.position;
        pos.y = wh;
        Dock2.transform.position = pos;
    }

    /// <summary>
    /// Shorthand for caching the pick up object of a weapon and retrieving it according to the weapon type
    /// </summary>
    /// <param name="obj">The game object of the weapon</param>
    /// <param name="type">The type of the weapon. Determines which variable to cache</param>
    private Script_PickUpObject GetPickUpObject(GameObject obj, WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Gun:
                return _scriptPickUpObjGun ?? (_scriptPickUpObjGun = obj.GetComponent<Script_PickUpObject>());

            case WeaponType.Sword:
                return _scriptPickUpObjSword ?? (_scriptPickUpObjSword = obj.GetComponent<Script_PickUpObject>());

            default:
                return obj.GetComponent<Script_PickUpObject>() ?? obj.GetComponentInParent<Script_PickUpObject>();
        }
    }

    /// <summary>
    /// Shorthand for caching the box collider of a weapon and retrieving it according to the weapon type
    /// </summary>
    /// <param name="obj">The game object of the weapon</param>
    /// <param name="type">The type of the weapon. Determines which variable to cache</param>
    private BoxCollider GetBoxCollider(GameObject obj, WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Gun:
                return _colliderGun ?? (_colliderGun = obj.GetComponent<BoxCollider>());

            case WeaponType.Sword:
                return _colliderSword ?? (_colliderSword = obj.GetComponent<BoxCollider>());

            default:
                return obj.GetComponent<BoxCollider>() ?? obj.GetComponentInParent<BoxCollider>();
        }
    }

    private Rigidbody GetRigidbody(GameObject obj, WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Gun:
                return _rigidGun ?? (_rigidGun = obj.GetComponent<Rigidbody>());

            case WeaponType.Sword:
                return _rigidSword ?? (_rigidSword = obj.GetComponent<Rigidbody>());

            default:
                return obj.GetComponent<Rigidbody>() ?? obj.GetComponentInParent<Rigidbody>();
        }
    }

    private WeaponType GetWeaponType(GameObject obj)
    {
        if (obj.CompareTag(TagGun))
            return WeaponType.Gun;

        if (obj.CompareTag(TagSword))
            return WeaponType.Sword;

        return WeaponType.None;
    }

    private void UpdateDockMesh(GameObject dock, MeshFilter filter, WeaponType type)
    {
        // Set mesh
        filter.mesh = (type == WeaponType.Gun) ? _meshGun : _meshSword;

        // Set transform
        filter.transform.localScale = (type == WeaponType.Gun) ? _scaleGun * Vector3.one : _scaleSword * Vector3.one;
        filter.transform.localPosition = (type == WeaponType.Gun) ? _offsetGun : _offsetSword;

        dock.transform.localEulerAngles = (type == WeaponType.Gun) ? _rotationGun : _rotationSword;
    }

    private void Dock1Enter(Collider col)
    {
        if (_scriptPickUpObjDock2 != null && col.gameObject == _scriptPickUpObjDock2.gameObject)
            return;

        var type = GetWeaponType(col.gameObject);
        if (type == WeaponType.None || !_dock1Free)
            return;

        //Check if weapon is in teleporter at the same time
        var wep = col.GetComponent<Script_Weapon>();
        if (wep != null && wep.IsInTeleporter)
            return;

        _typeDock1 = type;

        // Haptic pulse
        _scriptFeedback.SendShortVib(_hapticPulseStrength, GetPickUpObject(col.gameObject, _typeDock1).ControlHandSide);

        // Set color
        _rendererDock1.material.SetColor(_shaderID, _colorInitDock);

        //Clear actions if another weapon was in dock
        if (_scriptPickUpObjDock1 != null)
            _scriptPickUpObjDock1.OnRelease -= DockObjectIn1;

        //Subscribe to action when weapon is released -> Dock in dock 1
        _scriptPickUpObjDock1 = GetPickUpObject(col.gameObject, _typeDock1);
        _scriptPickUpObjDock1.OnRelease += DockObjectIn1;

        // Set docking station mesh
        UpdateDockMesh(Dock1, _meshFilterDock1, _typeDock1);
    }

    private void Dock2Enter(Collider col)
    {
        if (_scriptPickUpObjDock1 != null && col.gameObject == _scriptPickUpObjDock1.gameObject)
            return;

        var type = GetWeaponType(col.gameObject);
        if (type == WeaponType.None || !_dock2Free)
            return;

        //Check if weapon is in teleporter at same time
        var wep = col.GetComponent<Script_Weapon>();
        if (wep != null && wep.IsInTeleporter)
            return;

        _typeDock2 = type;

        // Haptic pulse
        _scriptFeedback.SendShortVib(_hapticPulseStrength, GetPickUpObject(col.gameObject, _typeDock2).ControlHandSide);

        // Set color
        _rendererDock2.material.SetColor(_shaderID, _colorInitDock);

        //Clear actions if another weapon was in dock
        if (_scriptPickUpObjDock2 != null)
            _scriptPickUpObjDock2.OnRelease -= DockObjectIn2;

        //Subscribe to action when weapon is released -> Dock in dock 2
        _scriptPickUpObjDock2 = GetPickUpObject(col.gameObject, _typeDock2);
        _scriptPickUpObjDock2.OnRelease += DockObjectIn2;

        // Set docking station mesh
        UpdateDockMesh(Dock2, _meshFilterDock2, _typeDock2);
    }

    private void Dock1Leave(Collider col)
    {
        var weapType = GetWeaponType(col.gameObject);
        if (_typeDock1 != WeaponType.None && weapType != WeaponType.None && weapType == _typeDock1)
        {
            // Set color
            _rendererDock1.material.SetColor(_shaderID, Color.clear);

            //Unsubscribe from action weapon release -> Weapon doesn't get docked always when released
            _scriptPickUpObjDock1.OnRelease -= DockObjectIn1;
            _typeDock1 = WeaponType.None;
            _scriptPickUpObjDock1 = null;
        }
    }

    private void Dock2Leave(Collider col)
    {
        var weapType = GetWeaponType(col.gameObject);
        if (_typeDock2 != WeaponType.None && weapType != WeaponType.None && weapType == _typeDock2)
        {
            // Set color
            _rendererDock2.material.SetColor(_shaderID, Color.clear);

            //Unsubscribe from action weapon release -> Weapon doesn't get docked always when released
            _scriptPickUpObjDock2.OnRelease -= DockObjectIn2;
            _scriptPickUpObjDock2 = null;
            _typeDock2 = WeaponType.None;
        }
    }

    private void DockObjectIn1(GameObject obj)
    {
        if (_scriptPickUpObjDock2 != null && obj == _scriptPickUpObjDock2.gameObject)
            return;

        if (obj == null || Dock1 == null || !_dock1Free)
            return;

        //Don't dock when releasing after docking already
        if (_scriptPickUpObjDock1 != null)
            _scriptPickUpObjDock1.OnRelease -= DockObjectIn1;

        _typeDock1 = GetWeaponType(obj);

        //Put weapon in center of dock
        DockObject(obj, Dock1, _typeDock1);

        // Set highlight color
        _rendererDock1.material.SetColor(_shaderID, Color.clear);

        //Subscribe to action when weapon is grabbed -> Release from dock
        _scriptPickUpObjDock1 = GetPickUpObject(obj, _typeDock1);

        _scriptPickUpObjDock1.OnGrab += Undock1;

        //Adjust weapon collider to be over entire dock for easier grabbing
        var c = GetBoxCollider(obj, _typeDock1);
        _dock1ExtentsSave = c.size; //save original weapon collider size
        var docColSize = _colliderDock1.size;

        //Adjust collider size for weapon scale
        c.size = new Vector3(docColSize.x / obj.transform.localScale.x, docColSize.y / obj.transform.localScale.y, docColSize.z / obj.transform.localScale.z);
        _dock1Free = false;

        var wep = obj.GetComponent<Script_Weapon>();
        if (wep != null && wep.IsInTeleporter)
        {
            Undock1(obj);
        }
    }

    private void DockObjectIn2(GameObject obj)
    {
        if (_scriptPickUpObjDock1 != null && obj == _scriptPickUpObjDock1.gameObject)
            return;

        if (obj == null || Dock2 == null || !_dock2Free)
            return;

        //Don't dock when releasing after docking already
        if (_scriptPickUpObjDock2 != null)
            _scriptPickUpObjDock2.OnRelease -= DockObjectIn2;

        _typeDock2 = GetWeaponType(obj);

        //Put weapon in center of dock
        DockObject(obj, Dock2, _typeDock2);

        // Set highlight color
        _rendererDock2.material.SetColor(_shaderID, Color.clear);

        //Subscribe to action when weapon is grabbed -> Release from dock
        _scriptPickUpObjDock2 = GetPickUpObject(obj, _typeDock2);

        _scriptPickUpObjDock2.OnGrab += Undock2;

        //Adjust weapon collider to be over entire dock for easier grabbing
        var c = GetBoxCollider(obj, _typeDock2);
        _dock2ExtentsSave = c.size; //save original weapon collider size
        var docColSize = _colliderDock2.size;

        //Adjust collider size for weapon scale
        c.size = new Vector3(docColSize.x / obj.transform.localScale.x, docColSize.y / obj.transform.localScale.y, docColSize.z / obj.transform.localScale.z); //Dock2.GetComponent<BoxCollider>().size / transform.localScale;
        _dock2Free = false;

        var wep = obj.GetComponent<Script_Weapon>();
        if (wep != null && wep.IsInTeleporter)
        {
            Undock2(obj);
        }
    }

    private void Undock1(GameObject obj)
    {
        if (obj != null && Dock1 != null)
        {
            //Unsubscribe from grab action
            if (_scriptPickUpObjDock1 != null)
                _scriptPickUpObjDock1.OnGrab -= Undock1;

            //Reset weapon collider
            GetBoxCollider(obj, _typeDock1).size = _dock1ExtentsSave;

            _dock1Free = true;
            _typeDock1 = WeaponType.None;

            //Clear link and cache from weapon to dock
            UnDockObject(obj, ref _typeDock1, ref _scriptPickUpObjDock1);
        }
    }

    private void Undock2(GameObject obj)
    {
        if (obj != null && Dock2 != null)
        {
            //Unsubscribe from grab action
            if (_scriptPickUpObjDock2 != null)
                _scriptPickUpObjDock2.OnGrab -= Undock2;

            //Reset weapon collider
            GetBoxCollider(obj, _typeDock2).size = _dock2ExtentsSave;

            _dock2Free = true;
            _typeDock2 = WeaponType.None;

            //Clear link and cache from weapon to dock
            UnDockObject(obj, ref _typeDock2, ref _scriptPickUpObjDock2);
        }
    }

    private void DockObject(GameObject obj, GameObject dock, WeaponType type = WeaponType.None)
    {
        //Match transforms
        obj.transform.parent = dock.transform;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        // Compensate for wrong gun axis
        if (type == WeaponType.Gun)
        {
            obj.GetComponent<Script_Gun>().ShowVisualModEffects(false);
            obj.transform.localEulerAngles = _rotationOffsetGun;
        }

        //Lock weapon in place
        obj.GetComponent<Rigidbody>().isKinematic = true;
    }

    private void UnDockObject(GameObject obj, ref WeaponType type, ref Script_PickUpObject pickUp)
    {
        //Make weapon free
        obj.transform.parent = null;
        if (pickUp == null)
            obj.GetComponent<Rigidbody>().isKinematic = false;
        else if (!pickUp.ToggleKinematicWhilstHeld)
            obj.GetComponent<Rigidbody>().isKinematic = false;

        if (type == WeaponType.Gun)
            obj.GetComponent<Script_Gun>().ShowVisualModEffects(true);

        //Clear cache
        type = WeaponType.None;
        if (pickUp != null)
        {
            pickUp.OnRelease -= DockObjectIn1;
            pickUp.OnRelease -= DockObjectIn2;
            pickUp = null;
        }
    }

    public void TeleportToFreeDock(GameObject obj)
    {
        //Dock object in first free dock
        if (_dock1Free && _scriptPickUpObjDock1 == null)
            DockObjectIn1(obj);
        else if (_dock2Free)
            DockObjectIn2(obj);
    }
}
