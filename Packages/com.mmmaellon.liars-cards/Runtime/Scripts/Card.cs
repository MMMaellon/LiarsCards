
using MMMaellon.LightSync;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.LiarsCards
{
    [RequireComponent(typeof(GroupTheory.Item))]
    [RequireComponent(typeof(SmoothTransition))]
    [RequireComponent(typeof(Animator))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Card : UdonSharpBehaviour
    {
        public LiarsCards game;
        public GroupTheory.Item item;
        public SmoothTransition transition;

        public Animator animator;

        [UdonSynced]
        public int _cardType;
        public int cardType
        {
            get => _cardType;
            set
            {
                _cardType = value;

                animator.enabled = true;
                animator.SetInteger("cardType", value);
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                }
            }
        }

        public void Setup(LiarsCards newGame)
        {
            game = newGame;
            transition.transitionPoints = new Transform[game.playerGroups.Length];
            for (int i = 0; i < game.playerGroups.Length; i++)
            {
                if (game.playerGroups[i].cardHolder)
                {
                    transition.transitionPoints[i] = game.playerGroups[i].cardHolder;
                }
                else
                {
                    transition.transitionPoints[i] = game.playerGroups[i].transform;
                }
            }
        }

        public void Reset()
        {
            item = GetComponent<GroupTheory.Item>();
            transition = GetComponent<SmoothTransition>();
            animator = GetComponent<Animator>();
        }

        public void SetVisible(bool modelVisible)
        {
            animator.enabled = true;
            animator.SetBool("modelVisible", modelVisible);
        }

        public void SetInteractive(bool interactive)
        {
            transition.sync.pickupableFlag = interactive;
            animator.enabled = true;

            animator.SetBool("interactive", interactive);
        }

        public void DisableAnimator()
        {
            animator.enabled = false;
        }
    }
}
