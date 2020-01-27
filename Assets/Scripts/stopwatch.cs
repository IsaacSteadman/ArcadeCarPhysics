
 using UnityEngine;
 using System.Collections;
 using UnityEngine.UI;
 
 public class stopwatch : MonoBehaviour
{
    //public Text timerLabel;
    public Text myText;
    private float time;
    void Start()
    {
        GameObject timerLabel = new GameObject("timerLabel");
        timerLabel.transform.SetParent(this.transform);
        myText = timerLabel.AddComponent<Text>();
        myText.color = Color.white;
        myText.fontSize = 24;
        myText.text = "Ta-dah!";

    }

    void Update()
    {
        time += Time.deltaTime;

        var minutes = time / 60; //Divide the guiTime by sixty to get the minutes.
        var seconds = time % 60;//Use the euclidean division for the seconds.
        var fraction = (time * 100) % 100;

        //update the label value
        myText.text = string.Format("{0:00} : {1:00} : {2:000}", minutes, seconds, fraction);
    }
}