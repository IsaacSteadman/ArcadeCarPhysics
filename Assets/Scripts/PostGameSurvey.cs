using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;

public class PostGameSurvey : MonoBehaviour
{
    private String[] questions = {
        "How would you rate the overall quality of the game (i.e. graphics, smoothness, responsiveness, etc.)?",
        "Some other questions?",
        "More questions?"
    };
    int qi = 0;
    int prev_qi = -1;
    GameObject qtext;
    ArcadeCar car;
    public bool showing = true;
    ToggleGroup toggle_group;
    GameObject panel;
    // Use this for initialization
    void Start()
    {
        qtext = GameObject.Find("PostSurveyQText");
        panel = GameObject.Find("Panel (Post-Game Survey)");
        car = GameObject.Find("Car").GetComponent<ArcadeCar>();
        car.postGameSurvey = this;
        toggle_group = GameObject.Find("PostSurveyToggleGroup").GetComponent<ToggleGroup>();
        hide();
    }

    public void hide()
    {
        showing = false;
        panel.SetActive(false);
    }

    public void show()
    {
        showing = true;
        panel.SetActive(true);
        prev_qi = -1;
        qi = 0;
    }

    public void nextAction()
    {
        if (!toggle_group.AnyTogglesOn())
        {
            return;
        }
        bool first = true;
        String dataRecord = "Question " + qi + " responses: ";
        foreach (Toggle t in toggle_group.ActiveToggles())
        {
            if (first)
            {
                first = false;
            }
            else
            {
                dataRecord += ", ";
            }
            dataRecord += t.gameObject.name;
        }
        ++qi;
        if (qi >= questions.Length)
        {
            qi = 0;
            car.donePostGameSurvey();
        }
        car.initLog();
        car.writer.WriteLine(dataRecord);
    }

    // Update is called once per frame
    void Update()
    {
        if (prev_qi != qi)
        {
            if (qi < questions.Length && qi >= 0)
            {
                qtext.GetComponent<UnityEngine.UI.Text>().text = questions[qi];
            }
            prev_qi = qi;
        }
    }
}
