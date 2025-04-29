using UnityEngine;

public class PickupFood : MonoBehaviour
{
    FirstPersonController player;
    void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<FirstPersonController>();
    }
    void OnTriggerEnter(Collider obj)
    {
        if (obj.CompareTag("Player"))
        {
            player.AddHunger();
            Destroy(gameObject);
            player.ShowDialog("Food. I needed that.", 2f);
        }
    }
}
