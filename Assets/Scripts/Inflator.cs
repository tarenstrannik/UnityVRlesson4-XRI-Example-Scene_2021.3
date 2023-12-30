using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Inflator : XRGrabInteractable
{
    [Header("Baloon Data")]
    [SerializeField] private Transform m_baloonAttachPoint;
    [SerializeField] private Balloon m_baloonPrefab;
    [SerializeField] private float m_minBaloonSize = 1f;
    [SerializeField] private float m_maxBaloonSize = 4f;
    private Balloon m_baloonInstance;


    private XRBaseController m_currentController;

    private float m_prevInput=0;


    [SerializeField] private float m_detachVelocity = 2f;
    [SerializeField] private float m_minVelocity = 0.25f;
    private bool m_isVelocityEnough=false;
    private Vector3 m_prevBaloonPosition;
    //[SerializeField] private TextMeshProUGUI m_test;

    [SerializeField] private Animator m_animator;


    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        var controllerInteractor = args.interactorObject as XRBaseControllerInteractor;
        m_currentController = controllerInteractor.xrController;
        CreateBaloon();
        m_animator.SetBool("b_inHand", true);


    }
    private void CreateBaloon()
    {
        m_baloonInstance = Instantiate(m_baloonPrefab, m_baloonAttachPoint);
        m_baloonInstance.transform.localScale = Vector3.one * m_minBaloonSize;
        m_prevBaloonPosition = m_baloonInstance.transform.position;
    }
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        Destroy(m_baloonInstance.gameObject);
        m_animator.SetBool("b_inHand", false);

    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);
        if (isSelected && m_currentController != null && updatePhase==0)
        {
            VibrateOnBlowing();
            OnVelocity();
            AnimateTrigger();
        }
    }
    private void VibrateOnBlowing()
    {
        
            m_baloonInstance.transform.localScale = Vector3.one * Mathf.Lerp(m_minBaloonSize, m_maxBaloonSize, m_currentController.activateInteractionState.value);

            var deltaInput = Mathf.Abs(m_prevInput - m_currentController.activateInteractionState.value) * 10;
            if (deltaInput > 0)
            {
                if (m_currentController.activateInteractionState.value > 0 && m_currentController.activateInteractionState.value < 1)
                {
                    m_currentController.SendHapticImpulse(Mathf.Clamp01(deltaInput), Time.deltaTime);
                }
                else
                {
                    m_currentController.SendHapticImpulse(Mathf.Clamp01(deltaInput), 0.1f);
                }
            }
            m_prevInput = m_currentController.activateInteractionState.value;


    }
    private void OnVelocity()
    {
        var velocity = (m_baloonInstance.transform.position - m_prevBaloonPosition).magnitude / Time.deltaTime;
        m_prevBaloonPosition = m_baloonInstance.transform.position;
        
        if (!m_isVelocityEnough)
        {
            
            if (velocity >= m_detachVelocity)
            {
                m_baloonInstance.Detach();
                CreateBaloon();
                m_isVelocityEnough = true;
            }
        }
        else
        {
            
            if (velocity< m_minVelocity)
            {
                m_isVelocityEnough = false;
            }
        }
    }

    private void AnimateTrigger()
    {
        m_animator.SetFloat("f_AnimStateTime", m_currentController.activateInteractionState.value);
        
    }
}
