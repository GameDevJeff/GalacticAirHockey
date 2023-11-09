using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System;

public class PanelManager : MonoBehaviour
{

    public Animator initiallyOpen;

    private int m_OpenParameterId;
    private Animator m_Open;
    //private GameObject m_PreviouslySelected;

    [SerializeField] private Button buttonOnline;
    [SerializeField] private Animator animatorOnline;
    private bool onNetwork = false;

    public Action<GameObject> closedWindow;

    const string k_OpenTransitionName = "Open";
    const string k_ClosedStateName = "Closed";

    public void OnEnable()
    {
        m_OpenParameterId = Animator.StringToHash(k_OpenTransitionName);

        if (initiallyOpen == null)
            return;

        OpenPanel(initiallyOpen);

        CheckNetworkStatus();
    }

    public void OpenPanel(Animator anim)
    {
        if (m_Open == anim)
            return;
        if (onNetwork && anim == animatorOnline)
            return;

        anim.gameObject.SetActive(true);
        //var newPreviouslySelected = EventSystem.current.currentSelectedGameObject;

        anim.transform.SetAsLastSibling();

        CloseCurrent();

        //m_PreviouslySelected = null;

        m_Open = anim;
        m_Open.SetBool(m_OpenParameterId, true);

        //GameObject go = FindFirstEnabledSelectable(anim.gameObject);

        SetSelected(null);
    }

    static GameObject FindFirstEnabledSelectable(GameObject gameObject)
    {
        GameObject go = null;
        var selectables = gameObject.GetComponentsInChildren<Selectable>(true);
        foreach (var selectable in selectables)
        {
            if (selectable.IsActive() && selectable.IsInteractable())
            {
                go = selectable.gameObject;
                break;
            }
        }
        return go;
    }

    public void CloseCurrent()
    {
        if (m_Open == null)
            return;

        m_Open.SetBool(m_OpenParameterId, false);
        //SetSelected(m_PreviouslySelected);
        StartCoroutine(DisablePanelDeleyed(m_Open));
        m_Open = null;

        CheckNetworkStatus();
    }

    public void Deactivate (GameObject gameObject)
    {
        gameObject.SetActive(false);
        closedWindow?.Invoke(gameObject);
    }

    IEnumerator DisablePanelDeleyed(Animator anim)
    {
        bool closedStateReached = false;
        bool wantToClose = true;
        while (!closedStateReached && wantToClose)
        {
            if (!anim.IsInTransition(0))
                closedStateReached = anim.GetCurrentAnimatorStateInfo(0).IsName(k_ClosedStateName);

            wantToClose = !anim.GetBool(m_OpenParameterId);

            yield return new WaitForEndOfFrame();
        }

        if (wantToClose)
            anim.gameObject.SetActive(false);
    }

    private void SetSelected(GameObject go)
    {
        EventSystem.current.SetSelectedGameObject(null);
    }



    public void NetworkStartServer()
    {
        NetworkManager.Singleton.StartServer();
        CloseCurrent();
    }

    public void NetworkStartHost()
    {
        NetworkManager.Singleton.StartHost();
        CloseCurrent();
    }

    public void NetworkSartClient()
    {
        NetworkManager.Singleton.StartClient();
        CloseCurrent();
    }

    public void NetworkStop()
    {
        if (onNetwork)
        {
            NetworkManager.Singleton.Shutdown();
            buttonOnline.GetComponentInChildren<Text>().text = "Online";
            onNetwork = false;
        }
    }

    private void CheckNetworkStatus()
    {
        if (NetworkManager.Singleton == null)
            return;
        
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost ||
            NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsClient)
        {
            buttonOnline.GetComponentInChildren<Text>().text = "Disconnect";
            onNetwork = true;
        }
        else
        {
            buttonOnline.GetComponentInChildren<Text>().text = "Online";
            onNetwork = false;

        }
    }
}
