using UnityEngine;

public class ResultMenuController : MonoBehaviour
{
    public void OnMenuClicked()
    {
        SoundManager.Instance.Play("Confirm");

        SceneTransitioner.Instance.LoadSceneSlide(SceneTransitioner.SceneName.Title);
    }

    public void OnQuitClicked()
    {
        SoundManager.Instance.Play("Confirm");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}