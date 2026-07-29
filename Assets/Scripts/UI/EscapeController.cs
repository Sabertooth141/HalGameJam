using UnityEngine;

public class EscapeController : MonoBehaviour
{
    public void OnEscapePressed()
    {
        SceneTransitioner.Instance.LoadSceneSlide(SceneTransitioner.SceneName.StageSelect);
    }
}
