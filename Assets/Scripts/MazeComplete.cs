using UnityEngine;

public class MazeComplete : MonoBehaviour
{
    private void OnTriggerEnter(Collider obj)
    {
        if (obj.CompareTag("Player"))
        {
            // Call the win screen
            GameManager.Instance.WinGame();
        }
    }
}
