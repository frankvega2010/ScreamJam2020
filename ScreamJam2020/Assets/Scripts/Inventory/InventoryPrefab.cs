﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryPrefab : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0f, 0.5f, 0f), Space.Self);
    }
}
