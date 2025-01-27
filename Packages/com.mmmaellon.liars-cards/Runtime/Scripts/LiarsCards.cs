
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.LiarsCards
{
    [RequireComponent(typeof(Animator))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class LiarsCards : UdonSharpBehaviour
    {
        [HideInInspector, FieldChangeCallback(nameof(aliveCount))]
        public int _aliveCount = 0;
        public int aliveCount
        {
            get => _aliveCount;
            set
            {
                _aliveCount = value;
                if (Networking.LocalPlayer.IsOwner(gameObject) && value < 2 && gameState != GAME_STATE_STOPPED)
                {
                    gameState = GAME_STATE_STOPPED;
                }
            }
        }
        public int cardsPerHand = 5;
        public Transform turnIndicator;
        public Card[] cards;
        public PlayerGroup[] playerGroups;

        int nextCardIndex = 0;

        [UdonSynced, FieldChangeCallback(nameof(prevPlayerIndex))]
        int _prevPlayerIndex = -1;//negative means no previous index
        public int prevPlayerIndex
        {
            get => _prevPlayerIndex;
            set
            {
                _prevPlayerIndex = value;

                var hasLivingPreviousPlayer = _prevPlayerIndex >= 0 && playerGroups[_prevPlayerIndex].alive;
                for (int i = 0; i < playerGroups.Length; i++)
                {
                    playerGroups[i].EnableBullshitButton(hasLivingPreviousPlayer);
                }
            }
        }

        [UdonSynced, FieldChangeCallback(nameof(nextPlayerIndex))]
        int _nextPlayerIndex = 0;
        public int nextPlayerIndex
        {
            get => _nextPlayerIndex;
            set
            {

                _nextPlayerIndex = value % playerGroups.Length;
                if (turnIndicator)
                {
                    turnIndicator.position = playerGroups[_nextPlayerIndex].transform.position;
                }
                for (int i = 0; i < playerGroups.Length; i++)
                {
                    playerGroups[i].EnableSubmissionButtons(i == nextCardIndex);
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }

        public int aceCount = 6;
        public int kingCount = 6;
        public int queenCount = 6;
        public int jackCount = 6;
        public int jokerCount = 4;
        public int devilCount = 1;
        [HideInInspector]
        public const int CARD_ACE = 0;
        [HideInInspector]
        public const int CARD_KING = 1;
        [HideInInspector]
        public const int CARD_QUEEN = 2;
        [HideInInspector]
        public const int CARD_JACK = 3;
        [HideInInspector]
        public const int CARD_JOKER = 4;
        [HideInInspector]
        public const int CARD_DEVIL = 5;
        public void Start()
        {
            for (int i = 0; i < playerGroups.Length; i++)
            {
                playerGroups[i].Setup(this, i);
            }
            foreach (var card in cards)
            {
                card.Setup(this);
            }
        }

        public Animator animator;
        public void Reset()
        {
            animator = GetComponent<Animator>();
        }

        const int GAME_STATE_STOPPED = 0;
        const int GAME_STATE_DEALING = 1;
        const int GAME_STATE_PLAYERPLAY = 2;
        const int GAME_STATE_REVEAL = 3;
        const int GAME_STATE_PLAYERSHOOT = 4;
        const int GAME_STATE_ROUNDCLEANUP = 5;

        float lastStateChange;
        [UdonSynced, FieldChangeCallback(nameof(gameState))]
        int _gameState;
        public int gameState
        {
            get => _gameState;
            set
            {
                lastStateChange = Time.timeSinceLevelLoad;
                OnChangeGameState(_gameState, value);
                _gameState = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }

        //0 is Aces
        //1 is Kings
        //2 is Queens
        //3 is Jacks
        //other hides the animator
        [UdonSynced, FieldChangeCallback(nameof(roundType))]
        int _roundType;
        public int roundType
        {
            get => _roundType;
            set
            {
                _roundType = value;
                animator.SetInteger("roundType", value);
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }

        public void StartGame()
        {
            if (gameState != GAME_STATE_STOPPED)
            {
                return;
            }

            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            gameState = GAME_STATE_DEALING;
        }

        public void EndGame()
        {
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            gameState = GAME_STATE_STOPPED;
        }

        public void OnChangeGameState(int oldState, int newState)
        {
            if (oldState == newState)
            {
                return;
            }

            switch (newState)
            {
                case GAME_STATE_DEALING:
                    {
                        if (Networking.LocalPlayer.IsOwner(gameObject))
                        {
                            roundType = Random.Range(0, 3);
                        }
                        else
                        {
                            animator.SetInteger("roundType", roundType);
                        }
                        StartDealingLoop();
                        break;
                    }

                case GAME_STATE_PLAYERPLAY:
                    {
                        if (Networking.LocalPlayer.IsOwner(gameObject))
                        {
                            NextPlayerToPlay();
                            prevPlayerIndex = -1;
                        }
                        break;
                    }
                case GAME_STATE_REVEAL:
                    {
                        //TODO reveal cards
                        //then enable gun on player that got it wrong
                        if (Networking.LocalPlayer.IsOwner(gameObject))
                        {
                            gameState = GAME_STATE_PLAYERSHOOT;
                        }
                        break;
                    }

                case GAME_STATE_PLAYERSHOOT:
                    {
                        //for now just enable all
                        foreach (var player in playerGroups)
                        {
                            if (Networking.LocalPlayer.IsOwner(player.gameObject))
                            {
                                player.gunEnabled = player.alive;
                            }
                        }
                        break;
                    }

                case GAME_STATE_ROUNDCLEANUP:
                    {
                        StartCleanupLoop();
                        animator.SetInteger("roundType", -1001);
                        //transfer ownership to someone who is still playing
                        foreach (var playerGroup in playerGroups)
                        {
                            if (playerGroup.alive)
                            {
                                if (Networking.LocalPlayer.IsOwner(playerGroup.gameObject))
                                {
                                    if (!Networking.LocalPlayer.IsOwner(gameObject))
                                    {
                                        Networking.SetOwner(Networking.LocalPlayer, gameObject);
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                        break;
                    }

                default: { break; }
            }
        }

        public void StartCleanupLoop()
        {
            lastCleanupIndex = -1;
            SendCustomEventDelayedFrames(nameof(CleanupLoop), 0);
        }

        int lastCleanupIndex = 0;
        public float cleanupAnimationSeconds = 2f;
        public void CleanupLoop()
        {
            if (gameState == GAME_STATE_ROUNDCLEANUP)
            {
                SendCustomEventDelayedFrames(nameof(CleanupLoop), 0);
            }

            if (lastStateChange + cleanupAnimationSeconds > Time.timeSinceLevelLoad)
            {
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    var nextIndex = Mathf.RoundToInt(cards.Length * (Time.timeSinceLevelLoad - lastStateChange) / cleanupAnimationSeconds);
                    if (nextIndex >= cards.Length || gameState != GAME_STATE_ROUNDCLEANUP)
                    {
                        nextIndex = cards.Length - 1;
                    }
                    for (int i = lastCleanupIndex + 1; i <= nextIndex; i++)
                    {
                        cards[i].transition.TransitionTo(-1001, Vector3.zero, Quaternion.identity);
                    }
                    lastCleanupIndex = nextIndex;
                }
                return;
            }

            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                foreach (var playerGroup in playerGroups)
                {
                    var items = playerGroup.GetItems();
                    for (int i = 0; i < items.Count; i++)
                    {
                        ((GroupTheory.Item)items[i].Reference).RemoveFromGroup(playerGroup);
                    }
                }
                if (aliveCount > 1)
                {
                    gameState = GAME_STATE_DEALING;
                }
                else
                {
                    gameState = GAME_STATE_STOPPED;
                }
            }
        }


        public void StartDealingLoop()
        {
            lastDealIndex = -1;
            playersToDeal.Clear();
            cardsToDeal.Clear();
            for (int i = 0; i < aceCount; i++)
            {
                cardsToDeal.Add(CARD_ACE);
            }
            for (int i = 0; i < kingCount; i++)
            {
                cardsToDeal.Add(CARD_KING);
            }
            for (int i = 0; i < queenCount; i++)
            {
                cardsToDeal.Add(CARD_QUEEN);
            }
            for (int i = 0; i < jackCount; i++)
            {
                cardsToDeal.Add(CARD_JACK);
            }
            for (int i = 0; i < jokerCount; i++)
            {
                cardsToDeal.Add(CARD_JOKER);
            }
            for (int i = 0; i < devilCount; i++)
            {
                cardsToDeal.Add(CARD_DEVIL);
            }
            foreach (var playerGroup in playerGroups)
            {
                if (playerGroup.alive)
                {
                    playersToDeal.Add(playerGroup);
                }
            }
            SendCustomEventDelayedFrames(nameof(DealingLoop), 0);
        }

        public float cardSpacing = 0.2f;
        int lastDealIndex = -1;

        DataList cardsToDeal = new DataList();

        DataList playersToDeal = new DataList();
        public float showRoundTypeDelaySeconds = 1f;
        public float dealAnimationSeconds = 1f;
        public void DealingLoop()
        {
            if (gameState == GAME_STATE_DEALING)
            {
                SendCustomEventDelayedFrames(nameof(DealingLoop), 0);
            }
            if (Time.timeSinceLevelLoad - lastStateChange < showRoundTypeDelaySeconds)
            {
                return;
            }

            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                if (playersToDeal.Count == 0)
                {
                    gameState = GAME_STATE_STOPPED;
                    return;
                }
                var nextIndex = Mathf.RoundToInt(playersToDeal.Count * cardsPerHand * (Time.timeSinceLevelLoad - (lastStateChange + showRoundTypeDelaySeconds)) / dealAnimationSeconds);
                if (nextIndex >= playersToDeal.Count * cardsPerHand || gameState != GAME_STATE_DEALING)
                {
                    nextIndex = (playersToDeal.Count * cardsPerHand) - 1;
                    if (gameState == GAME_STATE_DEALING)
                    {
                        gameState = GAME_STATE_PLAYERPLAY;
                    }
                }
                for (int i = lastDealIndex + 1; i <= nextIndex; i++)
                {
                    var playerGroup = (PlayerGroup)playersToDeal[Mathf.FloorToInt((i) / cardsPerHand)].Reference;
                    if (cardsToDeal.Count == 0)
                    {

                        cards[nextCardIndex].cardType = CARD_JOKER;
                    }
                    else
                    {
                        var cardTypeIndex = Random.Range(0, cardsToDeal.Count);
                        cards[nextCardIndex].cardType = cardsToDeal[cardTypeIndex].Int;
                        cardsToDeal.RemoveAt(cardTypeIndex);
                    }
                    playerGroup.AddItem(cards[nextCardIndex].item);
                    cards[nextCardIndex].transition.TransitionTo(playerGroup.playerId, Vector3.right * (cardSpacing * (-(cardsPerHand - 1) / 2f + (i % cardsPerHand))), Quaternion.identity);
                    nextCardIndex = (nextCardIndex + 1) % cards.Length;
                }
                lastDealIndex = nextIndex;
            }
        }


        public void OnPlayCard()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                OnPlayCardCallback();
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(OnPlayCardCallback));
            }
        }
        public void OnPlayCardCallback()
        {
            if (playerGroups[_nextPlayerIndex].alive)
            {
                prevPlayerIndex = _nextPlayerIndex;
            }
            else
            {
                prevPlayerIndex = -1;
            }
            NextPlayerToPlay();
        }

        public void CallBullshit()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                CallBullshitCallback();
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(CallBullshitCallback));
            }
        }

        public void CallBullshitCallback()
        {
            //TODO check if real
            gameState = GAME_STATE_PLAYERSHOOT;
        }

        public void OnShootGunCallback()
        {
            foreach (var player in playerGroups)
            {
                if (player.gunEnabled)
                {
                    //we're still waiting on someone to shoot themself
                    return;
                }
            }
            gameState = GAME_STATE_ROUNDCLEANUP;
        }

        public void NextPlayerToPlay()
        {
            for (int i = 0; i < playerGroups.Length - 1; i++)//don't loop all the way around
            {
                nextPlayerIndex++;
                if (playerGroups[nextPlayerIndex].alive)
                {
                    return;
                }
            }
            Debug.LogWarning("OH NO Somehow there's no players left. Maybe someone left mid-game.");
            gameState = GAME_STATE_STOPPED;
        }

        public void NextState()
        {
            Debug.LogWarning("state was " + gameState);
            gameState = (gameState + 1) % (GAME_STATE_ROUNDCLEANUP + 1);
            Debug.LogWarning("state is now: " + gameState);
        }

        public void StartTestGame()
        {
            foreach (var player in playerGroups)
            {
                player.alive = true;
            }
            StartGame();
        }

    }
}
