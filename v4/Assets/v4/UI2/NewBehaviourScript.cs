using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    public GameObject Mov1;
    public GameObject Mov2;
    // Start is called before the first frame update
    void Start()
    {
        Part1();
    }

    // Update is called once per frame
    void Update()
    {

    }
    void Part1()
    {
        Mov1.SetActive(true);
        Mov2.SetActive(false);
        Invoke("Part2", 0.5f);
    }
    void Part2()
    {
        Mov1.SetActive(false);
        Mov2.SetActive(true);
        Invoke("Part1", 0.5f);
    }

}
