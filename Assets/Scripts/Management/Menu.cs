using UnityEngine.SceneManagement;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public void FindMatch(string sceneToLoad)
    {
        if (SelectionData.prefabName == null 
            || SelectionData.deck.Count <20) 
            return;
        SceneManager.LoadScene(sceneToLoad);
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
