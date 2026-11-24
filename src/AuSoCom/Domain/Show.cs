using System.Text.Json.Serialization;

namespace AuSoCom.Domain;

public sealed class Show
{
    public List<Guid> SceneIds { get; set; } = [];

    [JsonIgnore]
    public int? CurrentSceneIndex { get; private set; }

    public Guid? CurrentSceneId =>
        CurrentSceneIndex is { } index && index >= 0 && index < SceneIds.Count
            ? SceneIds[index]
            : null;

    public void Add(Guid sceneId) => SceneIds.Add(sceneId);

    public void Remove(Guid sceneId)
    {
        var index = SceneIds.IndexOf(sceneId);
        RemoveAt(index);
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= SceneIds.Count) return;
        SceneIds.RemoveAt(index);
        if (CurrentSceneIndex == index)
            CurrentSceneIndex = null;
        else if (CurrentSceneIndex > index)
            CurrentSceneIndex--;
    }

    public bool MoveUp(Guid sceneId)
    {
        return MoveUpAt(SceneIds.IndexOf(sceneId));
    }

    public bool MoveUpAt(int index)
    {
        if (index <= 0) return false;
        if (index >= SceneIds.Count) return false;
        (SceneIds[index - 1], SceneIds[index]) = (SceneIds[index], SceneIds[index - 1]);
        if (CurrentSceneIndex == index)
            CurrentSceneIndex = index - 1;
        else if (CurrentSceneIndex == index - 1)
            CurrentSceneIndex = index;
        return true;
    }

    public bool MoveDown(Guid sceneId)
    {
        return MoveDownAt(SceneIds.IndexOf(sceneId));
    }

    public bool MoveDownAt(int index)
    {
        if (index < 0 || index >= SceneIds.Count - 1) return false;
        (SceneIds[index], SceneIds[index + 1]) = (SceneIds[index + 1], SceneIds[index]);
        if (CurrentSceneIndex == index)
            CurrentSceneIndex = index + 1;
        else if (CurrentSceneIndex == index + 1)
            CurrentSceneIndex = index;
        return true;
    }

    public Guid? Back()
    {
        if (SceneIds.Count == 0) return null;
        CurrentSceneIndex = CurrentSceneIndex is null ? 0 : Math.Max(0, CurrentSceneIndex.Value - 1);
        return CurrentSceneId;
    }

    public Guid? Next()
    {
        if (SceneIds.Count == 0) return null;
        if (CurrentSceneIndex is null)
            CurrentSceneIndex = 0;
        else if (CurrentSceneIndex < SceneIds.Count - 1)
            CurrentSceneIndex++;
        return CurrentSceneId;
    }

    public Guid? Start(Guid? sceneId = null)
    {
        if (sceneId is { } requestedSceneId)
        {
            var requestedIndex = SceneIds.IndexOf(requestedSceneId);
            if (requestedIndex >= 0)
                CurrentSceneIndex = requestedIndex;
        }

        return CurrentSceneId ?? StartAtIndex(0);
    }

    public Guid? StartAtIndex(int index)
    {
        if (index < 0 || index >= SceneIds.Count) return null;
        CurrentSceneIndex = index;
        return CurrentSceneId;
    }
}
