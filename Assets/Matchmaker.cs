using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.MultiplayerModels;

public class Matchmaker : MonoBehaviour
{
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject leaveQueueButton;
    [SerializeField] private TMP_Text queueStatusText;

    private string ticketId;
    private Coroutine pollTicketCoroutine;

    private const string QueueName = "DefaultQueue";

    public void StartMatchmaking()
    {
        playButton.SetActive(false);
        queueStatusText.text = "Submitting ticket";
        queueStatusText.gameObject.SetActive(true);

        PlayFabMultiplayerAPI.CreateMatchmakingTicket(
            new CreateMatchmakingTicketRequest
            {
                Creator = new MatchmakingPlayer
                {
                    Entity = new EntityKey
                    {
                        Id = ClientStartUp.EntityId,
                        Type = "title_player_account"
                    },
                    Attributes = new MatchmakingPlayerAttributes
                    {
                        DataObject = new { }
                    }
                },

                GiveUpAfterSeconds = 120,

                QueueName = QueueName
            },
            OnMatchmakingTicketCreated,
            OnMatchMakingError
        );
    }

    private void OnMatchmakingTicketCreated(CreateMatchmakingTicketResult result)
    {
        ticketId = result.TicketId;
        pollTicketCoroutine = StartCoroutine(PollTicket());

        leaveQueueButton.SetActive(true);
        queueStatusText.text = "Ticket Created";
    }

    private void OnMatchMakingError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    private IEnumerator PollTicket()
    {
        while (true)
        {
            PlayFabMultiplayerAPI.GetMatchmakingTicket(
                new GetMatchmakingTicketRequest
                {
                    TicketId = ticketId,
                    QueueName = QueueName
                },
                OnGetMatchmakingTicket,
                OnMatchMakingError
            );

            yield return new WaitForSeconds(6);

        }
    }

    private void OnGetMatchmakingTicket(GetMatchmakingTicketResult result)
    {
        queueStatusText.text = $"status: {result.Status}";

        switch(result.Status)
        {
            case "Matched":
                StopCoroutine(pollTicketCoroutine);
                StartMatch(result.MatchId);
                break;

            case "Canceled":
                break;
        }
    }

    private void StartMatch(string matchId)
    {
        queueStatusText.text = $"Starting Match";

        PlayFabMultiplayerAPI.GetMatch(
            new GetMatchRequest
            {
                MatchId = matchId,
                QueueName = QueueName
            },
            OnGetMatch,
            OnMatchMakingError
        );
    }

    private void OnGetMatch(GetMatchResult result)
    {
        queueStatusText.text = $"{result.Members[0].Entity.Id} vs {result.Members[1].Entity.Id}";
    }
}
