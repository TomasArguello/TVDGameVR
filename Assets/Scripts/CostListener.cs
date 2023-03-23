using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using CostItem = Tamu.Tvd.CostValue.CostItem;

#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    /**
     *  Track the total quantity and cost of certain types of objects that exist in the scene.
     */
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
	public class CostListener : MonoBehaviour
	{
        // Fields =================================================================================
        [Header("Listening")]
        [SerializeField] private IntReactiveProperty _quantity = new IntReactiveProperty();
        [SerializeField] private FloatReactiveProperty _totalCost = new FloatReactiveProperty();
        public int Quantity => _quantity.Value;
        public float Cost => _totalCost.Value;

        [Space]
        [SerializeField] private CostItem _filter;
        public CostItem Filter => _filter;

        [Header("Text")]
        [SerializeField] private Text _quantityText;
        [SerializeField] private Text _costText;
        // ========================================================================================

        // Mono ===================================================================================
        // ------------------------------------------------------------------------------
		void Awake()
		{
            if (_quantityText != null)
                _quantity.Subscribe(n => _quantityText.text = n.ToString()).AddTo(this);

            if (_costText != null)
                _totalCost.Subscribe(c => _costText.text = c.ToString("C")).AddTo(this);
		}
        // ------------------------------------------------------------------------------
        // ========================================================================================
		
        // Methods ================================================================================
        public void SubscribeToCost(CostValue cost)
        {
            if ((_filter | CostItem.None) != CostItem.None
                && (_filter & cost.ItemType) != cost.ItemType)
                return;

            float val = cost.Cost;
            _quantity.Value++;
            _totalCost.Value += val;

            cost.gameObject.OnDestroyAsObservable()
                .Subscribe(_ =>
                {
                    _quantity.Value--;
                    _totalCost.Value -= val;
                })
                .AddTo(this);
        }
        // ========================================================================================
	}
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
}