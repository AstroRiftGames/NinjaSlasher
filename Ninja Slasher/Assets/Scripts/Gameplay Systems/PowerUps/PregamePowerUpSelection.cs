using System;
using System.Collections.Generic;

[Serializable]
public class PregamePowerUpSelection
{
    private readonly List<PowerUpType> _selected = new List<PowerUpType>(4);

    public int Count => _selected.Count;

    public bool TrySelect(PowerUpType type)
    {
        for (int i = 0; i < _selected.Count; i++)
        {
            if (_selected[i] == type)
                return false;
        }
        _selected.Add(type);
        return true;
    }

    public bool TryDeselect(PowerUpType type)
    {
        for (int i = 0; i < _selected.Count; i++)
        {
            if (_selected[i] == type)
            {
                _selected.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public void Toggle(PowerUpType type)
    {
        if (!TryDeselect(type))
            TrySelect(type);
    }

    public bool IsSelected(PowerUpType type)
    {
        for (int i = 0; i < _selected.Count; i++)
        {
            if (_selected[i] == type)
                return true;
        }
        return false;
    }

    public void Clear()
    {
        _selected.Clear();
    }

    public PowerUpType GetSelection(int index)
    {
        return _selected[index];
    }
}
