using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ProgramManager : MonoBehaviour
{

    [SerializeField] private Button exitButton;

    void Start()
    {
        exitButton.onClick.AddListener(ExitProgram);
    }

    private void ExitProgram()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
