using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class RevButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

    public GameObject btn;
    public ArcadeCar car_script;

    // Use this for initialization
    void Start()
    {
        car_script = GameObject.Find("Car").GetComponent<ArcadeCar>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnPointerDown(PointerEventData eventData)
    {
        car_script.reverse_pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        car_script.reverse_pressed = false;
    }
}
