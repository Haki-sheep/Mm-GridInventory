using MmInventory;
using UnityEngine;

public class TestAudioAndAnima : MonoBehaviour,IGridAudioAndAnimation
{
    public void OnDeselectItem()
    {
        Debug.Log("OnDeselectItem");
    }

    public void OnMouseEnterItem()
    {
        Debug.Log("OnMouseEnterItem");
    }

    public void OnMouseExitItem()
    {
        Debug.Log("OnMouseExitItem");
    }

    public void OnSelectItem()
    {
        Debug.Log("OnSelectItem");
    }


}
