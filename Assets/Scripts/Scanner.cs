using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;


public class Scanner : XRGrabInteractable
{

    
    [Header("Scanner Data")]
    [SerializeField] private Animator m_scannerAnimator;
    [SerializeField] private LineRenderer m_scannerLaser;
    private AudioSource m_scannerAudio;

    [SerializeField] private AudioClip m_openingAudio;
    [SerializeField] private AudioClip m_closingAudio;
    [SerializeField] private float m_hapticsInterval = 0.5f;
    private XRBaseControllerInteractor m_curController;
    private Coroutine m_hapticsCoroutine;
    // Start is called before the first frame update

    [SerializeField] private ObjectMaterial[] m_rendererToChange;
    private bool m_isSelected = false;
    private bool m_isActivated = false;

    [SerializeField] private TextMeshProUGUI m_targetName;
    [SerializeField] private TextMeshProUGUI m_targetCoords;

    private Collider m_currentHitCollider;
    private Material m_defaultScannedMaterial;
    [SerializeField] Material m_materialToReplaceWhenScanning;

    protected override void Awake()
    {
        base.Awake();
        m_scannerAudio = GetComponent<AudioSource>();
        foreach (ObjectMaterial mat in m_rendererToChange)
        {
            mat.m_standartMaterial = mat.m_Renderer.material;
        }
        ScannerActivated(false);
    }
    [System.Serializable]
    private class ObjectMaterial
    {
        public Renderer m_Renderer;
        public Material m_changedMaterial;
        public Material m_standartMaterial;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        m_scannerAnimator.SetBool("Opened", true);
        m_scannerAudio.PlayOneShot(m_openingAudio);
        base.OnSelectEntered(args);
        m_curController = args.interactorObject.transform.gameObject.GetComponent<XRBaseControllerInteractor>();
        m_isSelected = true;
    }
    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        m_scannerAnimator.SetBool("Opened", false);
        m_scannerAudio.PlayOneShot(m_closingAudio);
        base.OnSelectExiting(args);

        ScannerActivated(false);
        ChangeMaterial(true);
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        ScannerActivated(true);
        
    }

    protected override void OnDeactivated(DeactivateEventArgs args)
    {
        base.OnDeactivated(args);
        ScannerActivated(false);
        
    }

    private IEnumerator IVariateHaptics()
    {
        float curTime = 0;
        float curHaptics = 0;
        while (true)
        {
            if (curTime < m_hapticsInterval / 2)
            {
                curHaptics = Mathf.Lerp(0, 1, curTime * 2 / m_hapticsInterval);
                curTime += Time.deltaTime;
            }
            else if(curTime>= m_hapticsInterval / 2 && curTime< m_hapticsInterval)
            {
                curHaptics = Mathf.Lerp(0, 1, 1 - (curTime -m_hapticsInterval / 2) * 2 / m_hapticsInterval);
                curTime += Time.deltaTime;
            }
            else
            {
                curTime = 0;
                curHaptics = 0;
            }
           
            m_curController.SendHapticImpulse(curHaptics, Time.deltaTime);
            yield return null;
        }
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);

        ChangeMaterial(false);
    }

    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        base.OnHoverExited(args);
        ChangeMaterial(true);

    }

    private void ChangeMaterial(bool changeBack)
    {
        if (changeBack)
        {
            foreach (ObjectMaterial objMat in m_rendererToChange)
            {
                objMat.m_Renderer.material = objMat.m_standartMaterial;
            }
        }
        else if(!m_isSelected)
        {
            foreach (ObjectMaterial objMat in m_rendererToChange)
            {
                objMat.m_Renderer.material = objMat.m_changedMaterial;
            }
        }
    }

    private void ScannerActivated(bool isActive)
    {
        m_scannerLaser.gameObject.SetActive(isActive);
        if(isActive)
        {
            m_scannerAudio.Play();
            m_hapticsCoroutine = StartCoroutine(IVariateHaptics());
        }
        else
        {
            m_scannerAudio.Pause();
            if (m_hapticsCoroutine != null) StopCoroutine(m_hapticsCoroutine);
            ResetLastActivatedRenderer();
        }

        if (!isActive)
            SetDefaultScannerText();
        m_targetCoords.gameObject.SetActive(isActive);
        m_isActivated = isActive;
    }
    private void SetDefaultScannerText()
    {
        m_targetName.SetText("Ready to Scan");
        m_targetCoords.SetText("");
    }
    private void ScanObjects()
    {
        RaycastHit hit;
        var maxVector = m_scannerLaser.transform.position + 1000 * m_scannerLaser.transform.forward;
        if (Physics.Raycast(m_scannerLaser.transform.position, m_scannerLaser.transform.forward, out hit))
        {
            m_targetName.SetText(hit.collider.name);
            m_targetCoords.SetText("Coordinates: "+hit.collider.transform.position.ToString()+". Distance: "+ Vector3.Distance(m_scannerLaser.transform.position,hit.point).ToString());
            maxVector= hit.point;
        }
        else
        {
            SetDefaultScannerText();
        }
        if(m_currentHitCollider != hit.collider)
        {
            if (m_currentHitCollider != null && m_defaultScannedMaterial != null)
            {
                if (m_currentHitCollider.gameObject.GetComponentInChildren<Renderer>() != null)
                m_currentHitCollider.gameObject.GetComponentInChildren<Renderer>().material = m_defaultScannedMaterial;
            }
            
            m_currentHitCollider = hit.collider;
            Renderer renderer = null;
            if(hit.collider!=null) renderer = hit.collider.gameObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                m_defaultScannedMaterial = renderer.material;
                renderer.material = m_materialToReplaceWhenScanning;
            }
        }
            m_scannerLaser.SetPosition(1, m_scannerLaser.transform.InverseTransformPoint(maxVector));

        
    }

    private void ResetLastActivatedRenderer()
    {
        if (m_currentHitCollider != null && m_defaultScannedMaterial != null)
        {
            if (m_currentHitCollider.gameObject.GetComponentInChildren<Renderer>() != null)
                m_currentHitCollider.gameObject.GetComponentInChildren<Renderer>().material = m_defaultScannedMaterial;
        }
        m_currentHitCollider = null;
        m_defaultScannedMaterial = null;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);
        if(m_isActivated)
            ScanObjects();
    }
}
