using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fff : MonoBehaviour
{
    // Перетащите сюда объект с MarketplaceUI в инспекторе
    public MarketplaceUI marketplaceUI;

    // Или найдите автоматически
    void Start()
    {
        if (marketplaceUI == null)
            marketplaceUI = FindObjectOfType<MarketplaceUI>();
        CallShowMarketplace();
    }

    // Вызов метода, например, по кнопке
    public void CallShowMarketplace()
    {
        if (marketplaceUI != null)
            marketplaceUI.ShowMarketplace();
    }
}
