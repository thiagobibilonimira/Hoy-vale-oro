using UnityEngine;

public class Movimiento : MonoBehaviour
{
    public float velocidad = 5f;
    public float aceleracion = 2f;
    public float tiempoFrenado = 1f;
    private Rigidbody2D rb;
    private float velocidadActual = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        Debug.Log("Auto listo");
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");

        if (h > 0)
        {
            velocidadActual = Mathf.MoveTowards(velocidadActual, velocidad, aceleracion * Time.fixedDeltaTime);
        }
        else if (h < 0)
        {
            velocidadActual = Mathf.MoveTowards(velocidadActual, -velocidad, aceleracion * Time.fixedDeltaTime);
        }
        else
        {
            velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, (velocidad / tiempoFrenado) * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector2(velocidadActual, rb.linearVelocity.y);
    }
}