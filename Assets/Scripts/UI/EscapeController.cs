using UnityEngine;
using UnityEngine.EventSystems;

public class EscapeController : MonoBehaviour
{
    public void OnEscapePressed()
    {
        SoundManager.Instance.Play("Confirm");

        if (SceneTransitioner.Instance.GetCurrentScene(SceneTransitioner.SceneName.StageSelect))
        {
            SceneTransitioner.Instance.LoadSceneSlide(SceneTransitioner.SceneName.Title);
            return;
        }

        SceneTransitioner.Instance.LoadSceneSlide(SceneTransitioner.SceneName.StageSelect);
    }
    public void OnResetPressed()
    {
        GameController.Instance.ResetScene();
        EventSystem.current.SetSelectedGameObject(null);
    }
}