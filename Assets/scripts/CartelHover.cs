using UnityEngine;
using UnityEngine.EventSystems;

public class CartelHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject cartelActivo;
    public float escalaHover = 1.2f;
    private Vector3 escalaOriginal;

    void Start()
    {
        escalaOriginal = cartelActivo.transform.localScale;
        cartelActivo.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        cartelActivo.SetActive(true);
        cartelActivo.transform.localScale = escalaOriginal * escalaHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        cartelActivo.SetActive(false);
        cartelActivo.transform.localScale = escalaOriginal;
    }
}