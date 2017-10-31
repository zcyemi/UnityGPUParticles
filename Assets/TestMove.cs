using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMove : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        transform.Translate(Mathf.Sin(Time.time*2.0f)*0.015f, Mathf.Sin(Time.time * 1.5f) * 0.01f, 0);
    }
}
