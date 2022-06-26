using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//link SpawnPlayersEditor to each SpawnPlayers object in the editor
[CustomEditor(typeof(SpawnPlayers))]

//extend SpawnPlayersEditor to the Editor class to give it access to OnSceneGUI
public class SpawnPlayersEditor : Editor
{
    //OnSceneGUI is executed by Editor objects every time the scene editor is interacted with
    //in this method, the spawnPoints instance variable of SpawnPlayers is used to create movable position handles in the scene editor
    //moving these handles will change the positions of the spawnPoints elements.
    public void OnSceneGUI()
    {
        //target is an object of type Object and is referenced by all Editor objects
        //casting target to the Editor object's linked type allows its GUI tools to read and write to its public properties
        SpawnPlayers sp = (SpawnPlayers)target;

        //references to the spawnPoints instance variable and the size and height needed to create a box indicating where a player will spawn
        Vector3[] p = sp.spawnPoints;
        Vector3 s = sp.playerSize;
        float h = sp.playerHeightOffset;
        //Vector3 versions of the height offset and player height size to use in expression with other Vector3 variables
        Vector3 o = new Vector3(0.0f, h, 0.0f);
        //the player's height (s.y) will only be ever used in one calculation, many of the inputs of which dont change
        //so, the calculation is done prematurely here, so it doesn't have to be redone in each iteration.
        Vector3 hs = -o + new Vector3(0.0f, s.y * 0.5f, 0.0f);

        for (int i = 0; i < p.Length; i++)
        {
            //use playerOffset to place the handle at the base of where the playerPrefab will spawn, 
            //then use playerOffset again to set the correct position back in spawnPoints
            //Handles.PositionHandle returns a Vector3 value containing the current position of the handle in the editor
            p[i] = Handles.PositionHandle(p[i] - o, Quaternion.identity) + o;

            //use playerOffset and playerSize to draw a cube representing the volume a playerPrefab will use when spawned
            Handles.DrawWireCube(p[i] + hs, s);
        }
    }
}
