using UnityEngine;
using UnityEngine.UI;


public class HeartManager : MonoBehaviour
{

    public static HeartManager Instance;


    public Image[] hearts;


    public Sprite fullHeart;
    public Sprite emptyHeart;


    private int lives;



    void Awake()
    {
        Instance = this;
    }



    void Start()
    {
        lives = hearts.Length;

        UpdateHearts();
    }



    public void LoseHeart()
    {

        lives--;


        UpdateHearts();



        if (lives <= 0)
        {
            UIManager.Instance.ShowGameOver();
        }

    }



    public bool HasLives()
    {
        return lives > 0;
    }


    void UpdateHearts()
    {

        for (int i = 0; i < hearts.Length; i++)
        {

            if (i < lives)
                hearts[i].sprite = fullHeart;

            else
                hearts[i].sprite = emptyHeart;

        }

    }

}