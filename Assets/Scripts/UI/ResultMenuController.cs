using UnityEngine;

public class ResultMenuController : MonoBehaviour
{
    public void OnMenuClicked()
    {
        SceneTransitioner.Instance.LoadSceneSlide(SceneTransitioner.SceneName.Title);
    }

    public void OnQuitClicked()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}