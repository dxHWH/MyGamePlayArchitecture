using GamePlayArchitecture;
using UnityEngine;


class EventA : AbstractEventArgs
{
    public int num1;
    public int num2;
}

class EventB : AbstractEventArgs
{
    public EventB(string str) { this.info = str; }
    public string info;
}

public class EventSystemTest : MonoBehaviour
{
    float t = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventSystem.Instance.Register<EventA>(callback);
        EventSystem.Instance.Register<EventB>(callback2);

        EventSystem.Instance.Trigger(new EventA());
    }

    // Update is called once per frame
    void Update()
    {
        t = t + Time.deltaTime;
        this.transform.position =new Vector3(0,0,0) + new Vector3(1, 1, 1) * Mathf.Sin(t);
    }

    void callback(EventA e)
    {
        Debug.Log(e.num1);
        EventSystem.Instance.Trigger(new EventB("hello"));
    }

    void callback2(EventB e)
    {
        Debug.Log(e.info);
        EventSystem.Instance.Trigger(new EventA());
    }
}
