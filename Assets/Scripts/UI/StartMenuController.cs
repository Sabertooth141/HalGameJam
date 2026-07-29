using UnityEngine;

public class StartMenuController : MonoBehaviour
{
    public void OnStartPressed()
    {
        Time.timeScale = 1;

        SoundManager.Instance.Play("Confirm");

        SceneTransitioner.Instance.LoadSceneSlide(SceneTransitioner.SceneName.StageSelect);
    }

    public void OnQuitPressed()
    {
        SoundManager.Instance.Play("Confirm");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}