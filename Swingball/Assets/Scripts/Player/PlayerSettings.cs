using System;
using UnityEditor;
using UnityEngine;


public class ConnectionString
{
    public Guid Id;
    public string Name;
    public bool Licensed;
}
public class PlayerSettings
{
    static public string Name { get; set; }
    static public GameObject Character { get; set; }
}
