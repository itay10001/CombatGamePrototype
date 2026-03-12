using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    public TextMeshProUGUI landingText;
    public TextMeshProUGUI angleText;

    private float displayTimer;
    private Color currentColor;
    private PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
    }

    void Update()
    {
        // Landing feedback fade
        if (displayTimer > 0)
        {
            displayTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(displayTimer);
            landingText.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
        }
        else
        {
            landingText.text = "";
        }

        // Live angle display
        if (player != null)
        {
            Vector3 moveDir = new Vector3(player.GetVelocity().x, 0, player.GetVelocity().z).normalized;
            Vector3 facingDir = new Vector3(player.transform.forward.x, 0, player.transform.forward.z).normalized;

            if (moveDir.magnitude > 0.1f)
            {
                float angle = Vector3.Angle(facingDir, moveDir);
                string quality = angle < 45f ? "DANGER" : angle < 90f ? "RISKY" : "GOOD";
                angleText.text = $"Angle: {Mathf.RoundToInt(angle)}°  [{quality}]";
                angleText.color = angle < 45f ? Color.red : angle < 90f ? Color.yellow : Color.green;
            }
            else
            {
                angleText.text = "";
            }
        }
    }

    public void ShowLanding(string quality)
    {
        landingText.text = quality;
        displayTimer = 1.5f;

        switch (quality)
        {
            case "CLEAN LANDING": currentColor = Color.green; break;
            case "BAD LANDING": currentColor = Color.yellow; break;
            case "VERY BAD LANDING": currentColor = Color.red; break;
        }

        landingText.color = currentColor;
    }
}
