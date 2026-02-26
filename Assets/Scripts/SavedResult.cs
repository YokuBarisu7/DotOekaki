using UnityEngine;


[System.Serializable]
public class SavedResult
{
    public string theme;
    public Texture2D texture;

    public SavedResult(string theme, Texture2D texture)
    {
        this.theme = theme;
        this.texture = texture;
    }
}