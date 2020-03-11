using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class StartButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

    public GameObject btnTxt;
    public ArcadeCar car_script;
    bool isNotFirst = true;

    // Use this for initialization
    void Start()
    {
        btnTxt = GameObject.Find("StartButtonText");
        car_script = GameObject.Find("Car").GetComponent<ArcadeCar>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnPointerDown(PointerEventData eventData)
    {
        car_script.start_pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        car_script.start_pressed = false;
    }

    public bool changeToTakeSurvey()
    {
        Debug.Log("hello survey" + btnTxt);
        Text txt = btnTxt.GetComponent<Text>();
        Debug.Log("got text");
        txt.text = "Take Survey";
        if (isNotFirst)
        {
            isNotFirst = false;
            Debug.Log("hello survey true");
            return false;
        }
        Debug.Log("hello survey false");
        return true;
    }
}
