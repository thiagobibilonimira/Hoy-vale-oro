using UnityEngine;

public class NPCTrigger : MonoBehaviour
{
    public DialogoManager dialogoManager;

    private bool playerNear = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
        }
    }

    private void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.F))
        {
            dialogoManager.AbrirDialogo();
        }
    }
}