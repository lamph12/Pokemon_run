﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/***
 * HoangTV 2/2020
 * Box = Popup + Panel
 * Bật các Popup nối tiếp => các Popup có thể hiện đè lên nhau
 * Từ Popup bật lên Panel => Popup phải đóng trước mới được mở Panel (vì Panel luôn luôn có layer nhỏ hơn Popup => không thể sắp xếp đè lên nhau)
 ***/
public class BoxController : MonoBehaviour
{
    public const int BASE_INDEX_LAYER = 20;
    public static BoxController Instance;

    [SerializeField] private BaseScene currentScene;

    public bool isLoadingShow;
    public bool isLockEscape;
    public UnityAction actionOnClosedOneBox;
    public UnityAction actionStackEmty;

    private readonly Stack<BaseBox> boxStack = new Stack<BaseBox>();

    /// <summary>
    ///     List chứa các Box thuộc dạng bắt buộc phải xem (Lưu lại ID Popup và hàm để mở Box)
    ///     Box save Hiện đi hiện lại đến khi nào user ấn đóng thì mới remove ra khỏi List
    /// </summary>
    private readonly Dictionary<string, UnityAction> lstActionSaveBox = new Dictionary<string, UnityAction>();

    protected void Awake()
    {
        Instance = this;
        gameObject.name = "BoxController";
        isLoadingShow = true;
    }


    protected virtual void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            //ProcessBackBtn();
        }

        if (Input.GetKeyDown(KeyCode.Space)) DebugStack();
    }

    public void GetBoxAsync(BaseBox instance, Action<BaseBox> OnLoadedFirst, Action<BaseBox> OnLoaded,
        string resourcePath)
    {
        StartCoroutine(GetBoxAsyncHandle(instance, OnLoadedFirst, OnLoaded, resourcePath));
    }

    private IEnumerator GetBoxAsyncHandle(BaseBox instance, Action<BaseBox> OnLoadedFirst, Action<BaseBox> OnLoaded,
        string resourcePath)
    {
        // Context.Waiting.ShowWaiting();
        if (instance == null)
        {
            var request = Resources.LoadAsync<BaseBox>(resourcePath);
            yield return request;
            instance = Instantiate(request.asset as BaseBox);
            if (OnLoadedFirst != null)
                OnLoadedFirst(instance);
        }

        if (OnLoaded != null)
            OnLoaded(instance);
        //  Context.Waiting.HideWaiting();
    }

    public void AddNewBackObj(BaseBox obj)
    {
        boxStack.Push(obj);
        SettingOderLayerPopup();
    }

    private void SettingOderLayerPopup()
    {
        if (boxStack == null && boxStack.Count <= 0)
            return;
        var lst_backObjs = boxStack.ToArray();
        var lenght = lst_backObjs.Length;
        var index = 0;
        for (var i = lenght - 1; i >= 0; i--) lst_backObjs[i].ChangeLayerHandle(ref index);
    }

    public void Remove()
    {
        if (boxStack.Count == 0)
            return;
        var obj = boxStack.Pop();
        if (boxStack.Count == 0)
            OnStackEmpty();

        SettingOderLayerPopup();
    }

    /// <summary>
    ///     Đang có Popup hiện
    /// </summary>
    /// <returns></returns>
    public bool IsShowingPopup()
    {
        var lst_backObjs = boxStack.ToArray();
        var lenght = lst_backObjs.Length;
        for (var i = lenght - 1; i >= 0; i--)
            if (lst_backObjs[i].isPopup)
                return true;

        return false;
    }


    public void DebugStack()
    {
        var lst_backObjs = boxStack.ToArray();
        var lenght = lst_backObjs.Length;
        for (var i = lenght - 1; i >= 0; i--)
            Debug.Log(" =============== " + lst_backObjs[i].gameObject.name + " ===============");
    }

    private void OnStackEmpty()
    {
        if (actionStackEmty != null)
            actionStackEmty();
    }

    public virtual void ProcessBackBtn()
    {
        if (isLoadingShow)
            return;

        if (isLockEscape)
            return;

        if (boxStack.Count != 0)
        {
            boxStack.Peek().Close();
        }
        else
        {
            if (!OpenBoxSave())
                OnPressEscapeStackEmpty();
        }
    }

    protected virtual void OnPressEscapeStackEmpty()
    {
        if (currentScene == null)
            currentScene = FindObjectOfType<BaseScene>();

        if (currentScene != null)
            currentScene.OnEscapeWhenStackBoxEmpty();
    }

    protected void OnLevelWasLoaded(int level)
    {
        if (currentScene == null)
            currentScene = FindObjectOfType<BaseScene>();
        if (boxStack != null)
            boxStack.Clear();

        OpenBoxSave();
    }

    /// <summary>
    ///     Check không có Popup hay panel nào được bật
    /// </summary>
    /// <returns></returns>
    public bool IsEmptyStackBox()
    {
        return boxStack.Count > 0 ? false : true;
    }

    #region Box Save

    private bool OpenBoxSave()
    {
        var isOpened = false;
        foreach (var save in lstActionSaveBox)
            if (save.Value != null)
            {
                isOpened = true;
                save.Value();
            }

        return isOpened;
    }

    public void AddBoxSave(string idPopup, UnityAction actionOpen)
    {
        if (lstActionSaveBox.ContainsKey(idPopup))
            lstActionSaveBox.Add(idPopup, actionOpen);
    }

    public void RemoveBoxSave(string idPopup)
    {
        if (lstActionSaveBox.ContainsKey(idPopup))
            lstActionSaveBox.Remove(idPopup);
    }

    #endregion
}