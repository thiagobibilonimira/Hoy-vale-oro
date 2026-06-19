using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform objetivo;
    public float limiteIzquierdo = 0f;   // hasta donde puede ir para la izquierda
    public float limiteDerecho = 10f;    // hasta donde puede ir para la derecha

    void LateUpdate()
    {
        if (objetivo == null) return;

        float nuevaX = objetivo.position.x;

        // Limitar la cámara entre los dos bordes
        nuevaX = Mathf.Clamp(nuevaX, limiteIzquierdo, limiteDerecho);

        transform.position = new Vector3(
            nuevaX,
            transform.position.y,
            transform.position.z
        );
    }
}