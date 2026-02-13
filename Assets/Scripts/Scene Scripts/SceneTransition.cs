using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
   public void LoadScene(int sceneName)
   {
       SceneManager.LoadScene(sceneName);
   }
}
