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
            int index = i;

            if (i != scenes.Length - 1)
                buttons[i].onClick.AddListener(() => { SceneManager.LoadScene(scenes[index].Index); });

            if (i == buttons.Length - 1)
                break;
        }

        buttons[buttons.Length - 1].onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
                    Application.Quit(); 
#endif
        });
    }
}
