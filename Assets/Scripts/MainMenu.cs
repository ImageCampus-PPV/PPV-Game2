using Systems.SceneLoader;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{

    [SerializeField] private SceneRef[] scenes;
    [SerializeField] private Button[] buttons;

    private void Awake()
    {
        for (int i = 0; i < scenes.Length; ++i)
        {
            if (i == buttons.Length)
                break;

            int index = i;

            buttons[i].onClick.AddListener(() => { LoadScene(scenes[index].Index); });
        }

        buttons[buttons.Length - 1].onClick.AddListener(ExitGame);
    }

    private void LoadScene(int index)
    {
        SceneManager.LoadScene(index);
    }

    private void ExitGame()
    {
        ;
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
                    Application.Quit(); 
#endif
    }
}
