using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.SceneManagement.SceneManager;

public class menu : MonoBehaviour
{
    public void start_game()
    {
        SceneManager.LoadScene("snake");
    }
}
