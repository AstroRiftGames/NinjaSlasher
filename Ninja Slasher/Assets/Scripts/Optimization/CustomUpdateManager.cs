using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CustomUpdateManager : MonoBehaviour
{
    public static CustomUpdateManager Instance { get; private set; }

    private readonly List<Action> _updateActions = new();
    private readonly List<Action> _fixedUpdateActions = new();
    private readonly List<Action> _lateUpdateActions = new();

    private readonly List<Action> _updateBuffer = new();
    private readonly List<Action> _fixedUpdateBuffer = new();
    private readonly List<Action> _lateUpdateBuffer = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Subscribe

    public void SubscribeToUpdate(Action action)
    {
        if (action == null || _updateActions.Contains(action))
            return;

        _updateActions.Add(action);
    }

    public void SubscribeToFixedUpdate(Action action)
    {
        if (action == null || _fixedUpdateActions.Contains(action))
            return;

        _fixedUpdateActions.Add(action);
    }

    public void SubscribeToLateUpdate(Action action)
    {
        if (action == null || _lateUpdateActions.Contains(action))
            return;

        _lateUpdateActions.Add(action);
    }

    #endregion

    #region Unsubscribe

    public void UnsubscribeFromUpdate(Action action)
    {
        _updateActions.Remove(action);
    }

    public void UnsubscribeFromFixedUpdate(Action action)
    {
        _fixedUpdateActions.Remove(action);
    }

    public void UnsubscribeFromLateUpdate(Action action)
    {
        _lateUpdateActions.Remove(action);
    }

    #endregion

    #region Unity Loops

    private void Update()
    {
        _updateBuffer.Clear();
        _updateBuffer.AddRange(_updateActions);

        foreach (var action in _updateBuffer)
            action.Invoke();
    }

    private void FixedUpdate()
    {
        _fixedUpdateBuffer.Clear();
        _fixedUpdateBuffer.AddRange(_fixedUpdateActions);

        foreach (var action in _fixedUpdateBuffer)
            action.Invoke();
    }

    private void LateUpdate()
    {
        _lateUpdateBuffer.Clear();
        _lateUpdateBuffer.AddRange(_lateUpdateActions);

        foreach (var action in _lateUpdateBuffer)
            action.Invoke();
    }

    #endregion
}