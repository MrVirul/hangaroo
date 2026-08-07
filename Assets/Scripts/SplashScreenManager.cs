using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreenManager : MonoBehaviour
{
    [SerializeField] private float splashDuration = 2f;
    [SerializeField] private string nextScene = "MainMenu";

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(splashDuration);

        SceneManager.LoadScene(nextScene);
    }
}