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

        // Live landing score display
        if (player != null)
        {
            Vector3 vel = player.GetVelocity();
            bool isMoving = new Vector3(vel.x, 0, vel.z).magnitude > 0.1f;

            if (isMoving)
            {
                float score = player.GetLandingScore();
                string quality = score < 0.33f ? "GOOD" : score < 0.66f ? "RISKY" : "DANGER";
                Color col = score < 0.33f ? Color.green : score < 0.66f ? Color.yellow : Color.red;
                angleText.text = $"Landing: {quality}  [{Mathf.RoundToInt(score * 100f)}%]";
                angleText.color = col;
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