using UnityEngine;

public class Movimiento : MonoBehaviour
{
    public float velocidad = 5f;
    public float aceleracion = 2f;
    public float tiempoFrenado = 1f;
    private Rigidbody2D rb;
    private float velocidadActual = 0f;

    private static Vector3? posicionGuardada = null;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (posicionGuardada.HasValue)
            transform.position = posicionGuardada.Value;
    }

    public static void GuardarPosicion(Vector3 pos)
    {
        posicionGuardada = pos;
    }

    void FixedUpdate()
    {
        float h = 0f;

        // Sin combustible: frenar gradualmente
        if (InventoryManager.CurrentFuel <= 0f)
        {
            velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, (velocidad / tiempoFrenado) * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(velocidadActual, rb.linearVelocity.y);
            return;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h = 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h = -1f;

        if (h > 0)
            velocidadActual = Mathf.MoveTowards(velocidadActual, velocidad, aceleracion * Time.fixedDeltaTime);
        else if (h < 0)
            velocidadActual = Mathf.MoveTowards(velocidadActual, -velocidad, aceleracion * Time.fixedDeltaTime);
        else
            velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, (velocidad / tiempoFrenado) * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(velocidadActual, rb.linearVelocity.y);
    }
}