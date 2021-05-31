using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDetails : MonoBehaviour
{
    [SerializeField] List<SceneDetails> connectedScenes;
    public bool isLoaded { get; private set; }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            Debug.Log($"Entered {gameObject.name}");

            LoadScene();
            GameController.Instance.SetCurrentScene(this);
            
            //load all connected scenes
            foreach (var scene in connectedScenes)
            {
                scene.LoadScene();
            }
            
            //Unload the scenes that are no longer connected
            if (GameController.Instance.PrevScene != null)
            {
                var prevLoadedScenes = GameController.Instance.PrevScene.connectedScenes;
                foreach (var scene in prevLoadedScenes)
                {
                    if(!connectedScenes.Contains(scene) && scene != this)
                        scene.UnloadScene();
                        
                }
            }
        }
    }

    public void LoadScene()
    {
        if (!isLoaded)
        {
            SceneManager.LoadSceneAsync(gameObject.name, LoadSceneMode.Additive);
            isLoaded = true;
        }
    }
    
    public void UnloadScene()
    {
        if (isLoaded)
        {
            SceneManager.UnloadSceneAsync(gameObject.name);
            isLoaded = false;
        }
    }
}
