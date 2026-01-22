using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScenes : MonoBehaviour
{
   public void GotoScene2(){
        SceneManager.LoadScene("Planet_scene");
   }
}
