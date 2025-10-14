using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class GameOverView : MonoBehaviour
{
    [SerializeField] private Text winnerText;
    [SerializeField] private Text countdownText;
    [SerializeField] private uint countdownStartTime;

    public void StartCountdownWithWinner(string winnerName, Action countdownComplete)
    {
        winnerText.text = $"{winnerName} wins!";

        gameObject.SetActive(true);

        StartCoroutine(CountdownCoroutine(countdownComplete));
    }

    private IEnumerator CountdownCoroutine(Action countdownComplete)
    {
        uint time = countdownStartTime;

        while(time > 0)
        {
            countdownText.text = $"Leaving game in {time}...";
            yield return new WaitForSeconds(1);
            time--;
        }

        countdownComplete?.Invoke();
    }

}
