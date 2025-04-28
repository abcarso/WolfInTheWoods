using UnityEngine;

public class MazeComplete : MonoBehaviour
{
    FirstPersonController player;
    public GameObject winPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<FirstPersonController>();

    }

    // Update is called once per frame
    void OnTriggerEnter(Collider obj)
    {
        if (obj.CompareTag("Player"))
        {
            winPanel.SetActive(true);
            player.DisablePlayer();
        }
    }
}
