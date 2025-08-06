public static class LevelSelectionUI
{
    public static bool ShouldLevelBeInteractable(int levelId)
    {
        return LevelProgressionManager.Instance?.IsLevelUnlocked(levelId) ?? false;
    }

    public static string GetLevelStatusText(int levelId)
    {
        if (!LevelProgressionManager.Instance.IsLevelUnlocked(levelId))
        {
            return "BLOQUEADO";
        }

        var saveData = SaveManager.Instance.GetGameData();
        if (saveData.levelStars.TryGetValue(levelId, out int stars))
        {
            return $"{stars}/3";
        }

        return "DISPONIBLE";
    }

    public static bool ShouldShowLock(int levelId)
    {
        return !LevelProgressionManager.Instance.IsLevelUnlocked(levelId);
    }
}