using UnityEngine;

public class StartMenuController : MonoBehaviour
{
    public void OnStartPressed()
    {
        Time.timeScale = 1;

        SceneTransitioner.Instance.LoadSceneSlide(SceneTransitioner.SceneName.StageSelect);
    }

    public void OnQuitPressed()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}