
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using MMMaellon.GroupTheory;

namespace MMMaellon.LiarsCards
{
    public class DeckGroup : IGroup
    {
        public LiarsCards game;
        public override void OnAddItem(Item item)
        {
            Card card = item.GetComponent<Card>();
            if (card)
            {
                card.SetVisible(true);
            }
        }

        public override void OnRemoveItem(Item item)
        {
            Card card = item.GetComponent<Card>();
            if (card)
            {
                card.SetVisible(false);
            }
        }
    }
}
