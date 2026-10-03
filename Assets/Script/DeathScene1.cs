using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Attach to an empty object in the DriftApartEnding scene.
public sealed class DeathScene1 : MonoBehaviour
{
    [SerializeField] private string gameScene = "MainScene";

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[Key.R].wasPressedThisFrame)
            SceneManager.LoadScene(gameScene);
    }
}
