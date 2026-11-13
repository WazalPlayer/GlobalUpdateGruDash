using System;

[Serializable]
public class EventData
{
    public int id;
    public string event_name;
    public int goal;
    public int current_deposit;
    public bool is_active;
    public int medal_id;
    public int event_image_index; // индекс второстепенного фото
}

[Serializable]
public class DepositRecord
{
    public int id;
    public int event_id;
    public string player_id;
    public int deposit_amount;
    public bool claimed;
    public string claimed_at;
}