using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public void GotoL1Scene()
    {
        SceneManager.LoadScene("Level_1");
    }
    public void GotoL2Scene()
    {
        SceneManager.LoadScene("Level_2");
    }
        public void GotoL3Scene()
    {
        SceneManager.LoadScene("Level_3");
    }
            public void GotoL4Scene()
    {
        SceneManager.LoadScene("Level_4");
    }
}