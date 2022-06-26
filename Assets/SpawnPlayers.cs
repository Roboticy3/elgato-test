using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviour
{
    //the prefab GameObject of a player
    public GameObject playerPrefab;

    //these two properties allow for the construction of cubes around elements of spawnPoints in the scene editor
    //these cubes indicate the volume the playerPrefab will use when spawning
    //the x, y, and z dimensions of playerPrefab
    public Vector3 playerSize;
    //the offset of the origin of playerPrefab from a point where it touches the ground
    public float playerHeightOffset;

    public Vector3[] spawnPoints;
 
    //spawn players in at the start of the scene
    private void Start()
    {
        PhotonNetwork.Instantiate(playerPrefab.name, SpawnPosition(), Quaternion.identity);
    }

    //return a random spawn position from the spawnPoints array
    private Vector3 SpawnPosition()
    {
        int spawnIndex = (int)(Random.Range(0.0f, (float)(spawnPoints.Length + 1)));
        return spawnPoints[spawnIndex];
    }

    //destroy PhotonNetwork's previous copy of a player and spawn a new one (?)
    //I don't totally understand what this does so we have to wait for Alex's sagelike knowledge of PhotonNetwork
    public void Respawn(PhotonView nuts)
    {
        PhotonNetwork.Destroy(nuts);
        Start();
    }

}
