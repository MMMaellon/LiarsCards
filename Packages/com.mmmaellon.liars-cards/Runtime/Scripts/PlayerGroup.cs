
using MMMaellon.GroupTheory;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.LiarsCards
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerGroup : IGroup
    {
        public int playerId = -1001;
        public LiarsCards game;
        public Transform cardHolder;
        public DeckGroup deck;

        [UdonSynced]
        public bool _alive;
        public bool alive
        {
            get => _alive;
            set
            {
                _alive = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }

                if (value)
                {
                    game.aliveCount++;
                }
                else
                {
                    game.aliveCount--;
                }
            }
        }

        [UdonSynced, FieldChangeCallback(nameof(gunEnabled))]
        public bool _gunEnabled;
        public bool gunEnabled
        {
            get => _gunEnabled;
            set
            {
                _gunEnabled = value;
                if (Networking.LocalPlayer.IsOwner(game.gameObject))
                {
                    game.OnShootGunCallback();
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }

        [UdonSynced]
        public int _bullets = 0;
        public int bullets
        {
            get => _bullets;
            set
            {
                _bullets = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        public override void OnAddItem(Item item)
        {
            Card card = item.GetComponent<Card>();
            if (card)
            {
                card.SetVisible(true);
                card.SetInteractive(Networking.LocalPlayer.IsOwner(gameObject));
            }
        }

        public override void OnRemoveItem(Item item)
        {
            Card card = item.GetComponent<Card>();
            if (card)
            {
                card.SetVisible(false);
                card.SetInteractive(false);
            }
        }

        public void EnableSubmissionButtons(bool enable)
        {
            if (enable)
            {

            }
            else
            {

            }
        }

        public void EnableBullshitButton(bool enable)
        {
            if (enable)
            {

            }
            else
            {

            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (alive)
            {
                alive = false;
            }
        }

        public void ShootGun()
        {
            if (!gunEnabled)
            {
                return;
            }
            gunEnabled = false;
            if (Random.Range(0, 6 - bullets) == 0)
            {
                Debug.LogWarning("Had a bullet");
                alive = false;
            }
            else
            {
                Debug.LogWarning("No bullet");
            }
            bullets++;
        }

        public void SubmitCards()
        {
            game.OnPlayCard();
        }

        public void Setup(LiarsCards newGame, int playerId)
        {
            game = newGame;
            this.playerId = playerId;
        }
    }
}
