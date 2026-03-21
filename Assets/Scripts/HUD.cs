using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    public TextMeshProUGUI landingText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI recoveryText;
    public TextMeshProUGUI stunText;

    private float displayTimer;
    private Color currentColor;
    private PlayerController player;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (recoveryText) recoveryText.gameObject.SetActive(false);
        if (stunText) stunText.gameObject.SetActive(false);
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

        // Live landing score
        if (player != null && player.IsAirborne())
        {
            Vector3 vel = player.GetVelocity();
            bool isFalling = vel.y < -2f;

            if (isFalling)
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
        else
        {
            angleText.text = "";
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
            case "RECOVERY!": currentColor = Color.cyan; break;
        }

        landingText.color = currentColor;
    }

    public void ShowRecovery()
    {
        if (recoveryText)
        {
            recoveryText.gameObject.SetActive(true);
            recoveryText.text = "Press E to recover!";
            recoveryText.color = Color.yellow;
        }
    }

    public void HideRecovery()
    {
        if (recoveryText)
            recoveryText.gameObject.SetActive(false);
    }

    public void ShowStun()
    {
        if (stunText)
        {
            stunText.gameObject.SetActive(true);
            stunText.text = "STUNNED";
            stunText.color = Color.red;
        }
    }

    public void HideStun()
    {
        if (stunText)
            stunText.gameObject.SetActive(false);
    }
}
