using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;

public class PostGameSurvey : MonoBehaviour
{
    private String[] questions = {
        "How would you rate the overall quality of the game (i.e. graphics, smoothness, responsiveness, etc.)? " +
            "[0 - Terrible; 10 - Awesome]",
        "How would you rate your personal, overall gameplay experience? " +
            "[0 - Terrible; 10 - Awesome]",
        "How would you rate this game's graphical resolution? " +
            "[0 - Terrible; 10 - Awesome]",
        "How would you rate this game's smoothness (affected by frames per second)? " +
            "[0 - Terrible; 10 - Awesome]",
        "How would you rate this game's responsiveness to user input (aka your actions and controls)? " +
            "[0 - Terrible; 10 - Awesome]",
        "How frustrated were you with the gameplay due to the game's quality/performance? " +
            "[0 - Not at all; 10 - Extremely]"
    };
    int qi = 0;
    int prev_qi = -1;
    GameObject qtext, etext, ebtn;
    ArcadeCar car;
    public bool showing = true;
    public bool doneSurvey = false;
    public bool notErrorYet = true;
    ToggleGroup toggle_group;
    GameObject panel;
    public String surveyData = "";
    // Use this for initialization
    void Start()
    {
        qtext = GameObject.Find("PostSurveyQText");
        etext = GameObject.Find("PostSurveyEText");
        ebtn = GameObject.Find("PostSurveyRetry");
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
        clearError();
    }

    public void show()
    {
        showing = true;
        panel.SetActive(true);
        toggle_group.SetAllTogglesOff();
        clearError();
        prev_qi = -1;
        qi = 0;
        surveyData = "";
    }

    static String getIdButtonName(String btnName)
    {
        int pos = btnName.IndexOf('(');
        if (pos < 0)
        {
            return "";
        }
        int pos1 = btnName.IndexOf(')', pos);
        if (pos1 < 0)
        {
            pos1 = btnName.Length;
        }
        return btnName.Substring(pos + 1, pos1 - (pos + 1));
    }

    public void setError(String error)
    {
        notErrorYet = false;
        etext.GetComponent<UnityEngine.UI.Text>().text = error;
        etext.SetActive(true);
        // ebtn.SetActive(true);
    }

    public void clearError()
    {
        notErrorYet = true;
        etext.SetActive(false);
        ebtn.SetActive(false);
    }

    public void nextAction()
    {
        if (!toggle_group.AnyTogglesOn())
        {
            return;
        }
        bool first = true;
        String arrRecord = "";
        foreach (Toggle t in toggle_group.ActiveToggles())
        {
            String id = getIdButtonName(t.gameObject.name);
            if (arrRecord.Length > 0)
            {
                arrRecord += ", ";
            }
            arrRecord += "\"" + id.Replace("\"", "\\\"") + "\"";
        }
        String dataRecord = "\"Question_" + qi + "\": [" + arrRecord + "]";
        if (surveyData.Length > 0)
        {
            surveyData += ", ";
        }
        else
        {
            surveyData = "{";
        }
        surveyData += dataRecord;
        ++qi;
        if (qi >= questions.Length)
        {
            qi = 0;
            doneSurvey = true;
            surveyData += "}";
        } else
        {
            toggle_group.SetAllTogglesOff();
        }
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
