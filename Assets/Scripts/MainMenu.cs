﻿using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject levelBrowserPanel;
    public LevelBrowser levelBrowser;

    public void Play()
    {
        mainMenu.SetActive(false);
        levelBrowserPanel.SetActive(true);
        levelBrowser.gameObject.SetActive(true);
        levelBrowser.LoadLevelList();
    }

    public void Create()
    {
        SceneManager.LoadScene("LevelEditor");
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Quit();
    }

}
