using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelNav : MonoBehaviour
{
    public void BackToMenu()
    {
        SceneManager.LoadScene("DisasterSelection");
    }
}
