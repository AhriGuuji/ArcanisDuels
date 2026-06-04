using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

public class Menu : MonoBehaviour
{
    [SerializeField] private TMP_InputField input;

    public void FindMatch(GameObject painel)
    {
        Debug.Log($"{SelectionData.prefabName} with {SelectionData.deck.Count} cards");
        if (string.IsNullOrEmpty(SelectionData.prefabName) 
            || SelectionData.deck.Count < 20) 
            return;

        OpenPainel(painel);
    }

    public void SendCode(string scene)
    {
        SelectionData.code = input.text;
        SceneManager.LoadScene(scene);
    }

    public void CreateMatch(string scene)
    {
        Debug.Log($"{SelectionData.prefabName} with {SelectionData.deck.Count} cards");

        if (string.IsNullOrEmpty(SelectionData.prefabName) 
            || SelectionData.deck.Count <20) 
            return;

        SelectionData.isServer = true;
        SceneManager.LoadScene(scene);
    }

    public void ChangeScene(string sceneToLoad)
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    public void OpenPainel(GameObject painel)
    {
        painel.SetActive(true);
    }

    public void ClosePainel(GameObject painel)
    {
        painel.SetActive(false);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
