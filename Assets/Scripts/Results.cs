using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class Results : MonoBehaviour
{

    //public GameObject scorebox;
    public Text textbox;
    public ArcadeCar car_script;

    // Use this for initialization
    void Start()
    {
        car_script = GameObject.Find("Car").GetComponent<ArcadeCar>();
        //textbox = scorebox.GetComponent<Text>();
        //textbox.text = "==========Scoreboard==========\n" + car_script.top5ScoresString;
        //textbox.text += car_script.top5ScoresString;
    }

    // Update is called once per frame
    void Update()
    {
        textbox.text = "Lap number: " + (car_script.lapCount+1).ToString();
    }
}
